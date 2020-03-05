using System;
using Unity.Collections;

namespace Lasp
{
    //
    // Input stream class
    //
    // This class provides a weak reference to an internal device handler
    // object. You can access the device information and the stream data
    // without manually managing the actual device object. The information will
    // be calculated when it's needed. The stream will be opened when you
    // access the data, and it will be closed when you stop accessing.
    // Everything is done in an on-demand fashion.
    //
    public sealed class InputStream
    {
        #region Stream settings

        public int ChannelCount => _deviceHandle.StreamChannelCount;
        public int SampleRate => _deviceHandle.StreamSampleRate;

        #endregion

        #region Per-channel audio levels

        public float GetChannelLevel(int channel)
            => _deviceHandle.GetChannelLevel(channel);

        #endregion

        #region Interleaved audio data (waveform)

        public ReadOnlySpan<float> AudioDataSpan
            => _deviceHandle.LastFrameWindow;

        public NativeSlice<float> AudioDataSlice
            => _deviceHandle.LastFrameWindow.GetNativeArray();

        #endregion

        #region Private and internal members

        InputDeviceHandle _deviceHandle;

        InputStream() {} // Hidden constructor

        internal static InputStream Create(InputDeviceHandle deviceHandle)
          => deviceHandle != null && deviceHandle.IsValid ?
            new InputStream { _deviceHandle = deviceHandle } : null;

        #endregion
    }
}
