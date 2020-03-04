using System;
using System.Runtime.InteropServices;
using InvalidOp = System.InvalidOperationException;
using PInvokeCallbackAttribute = AOT.MonoPInvokeCallbackAttribute;

namespace Lasp
{
    sealed class InputDeviceHandle : System.IDisposable
    {
        #region Public properties and methods

        public SoundIO.Device SioDevice => _device;
        public bool IsValid => _device != null;

        public bool IsStreamActive
            => _stream != null && !_stream.IsInvalid && !_stream.IsClosed;

        public int ChannelCount
            => IsStreamActive ? _stream.Layout.ChannelCount : 0;

        public int SampleRate
            => IsStreamActive ? _stream.SampleRate : 0;

        public float Latency
            => IsStreamActive ? (float)_stream.SoftwareLatency : 0;

        public float AudioRmsLevel => Prepare() ? _audioRmsLevel : 0;

        public ReadOnlySpan<byte> LastFrameWindow =>
            PrepareAndGetLastFrameWindow();

        bool Prepare()
        {
            if (!IsStreamActive)
            {
                OpenStream();
                return false;
            }
            else
            {
                return true;
            }
        }

        ReadOnlySpan<byte> PrepareAndGetLastFrameWindow()
        {
            if (!IsStreamActive) OpenStream();
            return new ReadOnlySpan<byte>(_window, 0, _windowSize);
        }

        public static InputDeviceHandle CreateAndOwn(SoundIO.Device device)
        {
            return new InputDeviceHandle(device);
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _stream = null;

            _device.Dispose();
            _device = null;

            _self.Free();
        }

        public void Update(float deltaTime)
        {
            // Nothing to do when the stream is inactive.
            if (!IsStreamActive) return;

            // Last frame window size
            _windowSize =
                Math.Min(_window.Length, CalculateBufferSize(deltaTime));

            lock (_ring)
            {
                // Copy the last frame data into the window buffer.
                if (_ring.FillCount >= _windowSize)
                    _ring.Read(new Span<byte>(_window, 0, _windowSize));
                else
                    _windowSize = 0; // Underflow

                // Reset the buffer when it detects an overflow.
                // TODO: Is this the best strategy to deal with overflow?
                if (_ring.OverflowCount > 0) _ring.Clear();
            }

            _audioRmsLevel = CalculateRMS();
        }

        float _audioRmsLevel;

        float CalculateRMS()
        {
            var data = MemoryMarshal.Cast<byte, float>(LastFrameWindow);
            var stride = ChannelCount;

            if (data.Length == 0) return 0;

            var sq_sum = 0.0f;
            for (var i = 0; i < data.Length; i += stride)
                sq_sum += data[i] * data[i];

            return Unity.Mathematics.math.sqrt(sq_sum / data.Length);
        }

        public void OpenStream()
        {
            if (IsStreamActive)
                throw new InvalidOp("Stream alreadly opened");

            try
            {
                _stream = SoundIO.InStream.Create(_device);

                if (_stream.IsInvalid)
                    throw new InvalidOp("Stream allocation error");

                if (_device.Layouts.Length == 0)
                    throw new InvalidOp("No channel layout");

                // Calculate the best latency.
                // TODO: Should we use the target frame rate instead of 1/60?
                var bestLatency = Math.Max(1.0 / 60, _device.SoftwareLatencyMin);

                // Stream properties
                _stream.Format = SoundIO.Format.Float32LE;
                _stream.Layout = _device.Layouts[0];
                _stream.SoftwareLatency = bestLatency;
                _stream.ReadCallback = _readCallback;
                _stream.OverflowCallback = _overflowCallback;
                _stream.ErrorCallback = _errorCallback;
                _stream.UserData = GCHandle.ToIntPtr(_self);

                var err = _stream.Open();

                if (err != SoundIO.Error.None)
                    throw new InvalidOp($"Stream initialization error ({err})");

                // We want the buffers to meet the following requirements:
                // - Doesn't overflow if the main thread pauses for 4 frames.
                // - Doesn't overflow if the callback is invoked 4 times a frame.
                var latency = Math.Max(_stream.SoftwareLatency, bestLatency);
                var bufferSize = CalculateBufferSize((float)(latency * 4));

                // Ring/window buffer allocation
                _ring = new RingBuffer(bufferSize);
                _window = new byte[bufferSize];

                // Start streaming.
                _stream.Start();
            }
            catch
            {
                // Dispose the stream on an exception.
                _stream?.Dispose();
                _stream = null;
                throw;
            }
        }

        public void CloseStream()
        {
            if (!IsStreamActive)
                throw new InvalidOp("Stream not opened");

            _stream?.Dispose();
            _stream = null;
        }

        #endregion

        #region Private objects

        // A GC handle used to share 'this' pointer with unmanaged code
        GCHandle _self;

        // SoundIO objects
        SoundIO.Device _device;
        SoundIO.InStream _stream;

        // Input stream ring buffer
        // This object will be accessed from both the main/callback thread.
        // Must be locked when accessing it.
        RingBuffer _ring;

        // Buffer for the last frame window
        byte[] _window;
        int _windowSize;

        #endregion

        #region Private properties and methods

        int CalculateBufferSize(float second)
            => (int)(_stream.SampleRate * second) *
               _stream.Layout.ChannelCount * sizeof(float);

        InputDeviceHandle(SoundIO.Device device)
        {
            _self = GCHandle.Alloc(this);
            _device = device;
        }

        #endregion

        #region SoundIO callback delegates

        static SoundIO.InStream.ReadCallbackDelegate
            _readCallback = OnReadInStream;

        static SoundIO.InStream.OverflowCallbackDelegate
            _overflowCallback = OnOverflowInStream;

        static SoundIO.InStream.ErrorCallbackDelegate
            _errorCallback = OnErrorInStream;

        [PInvokeCallback(typeof(SoundIO.InStream.ReadCallbackDelegate))]
        unsafe static void OnReadInStream
            (ref SoundIO.InStreamData stream, int min, int left)
        {
            // Recover the 'this' reference from the UserData pointer.
            var self = (InputDeviceHandle)
                GCHandle.FromIntPtr(stream.UserData).Target;

            while (left > 0)
            {
                // Start reading the buffer.
                var count = left;
                SoundIO.ChannelArea* areas;
                stream.BeginRead(out areas, ref count);

                // When getting count == 0, we must stop reading
                // immediately without calling InStream.EndRead.
                if (count == 0) break;

                if (areas == null)
                {
                    // We must do zero-fill when receiving a null pointer.
                    lock (self._ring)
                        self._ring.WriteEmpty(stream.BytesPerFrame * count);
                }
                else
                {
                    // Determine the memory span of the input data with
                    // assuming the data is tightly packed.
                    // TODO: Is this assumption always true?
                    var span = new ReadOnlySpan<Byte>(
                        (void*)areas[0].Pointer,
                        areas[0].Step * count
                    );

                    // Transfer the data to the ring buffer.
                    lock (self._ring) self._ring.Write(span);
                }

                stream.EndRead();

                left -= count;
            }
        }

        [PInvokeCallback(typeof(SoundIO.InStream.OverflowCallbackDelegate))]
        static void OnOverflowInStream(ref SoundIO.InStreamData stream)
            => UnityEngine.Debug.LogWarning("InStream overflow");

        [PInvokeCallback(typeof(SoundIO.InStream.ErrorCallbackDelegate))]
        static void OnErrorInStream
            (ref SoundIO.InStreamData stream, SoundIO.Error error)
            => UnityEngine.Debug.LogError($"InStream error ({error})");

        #endregion
    }
}
