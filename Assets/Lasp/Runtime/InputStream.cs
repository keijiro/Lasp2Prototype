using System;
using System.Runtime.InteropServices;
using Unity.Collections;

namespace Lasp
{
    public enum FilterType { Bypass, LowPass, BandPass, HighPass }

    public sealed class InputStream
    {
        public DeviceDescriptor Device { get; private set; }
        public FilterType FilterType { get; set; }

        public ReadOnlySpan<float> AudioDataSpan
            => MemoryMarshal.Cast<byte, float>(_deviceHandle.LastFrameWindow);

        public NativeSlice<float> AudioDataSlice => default(NativeSlice<float>);

        public float AudioRmsLevel => 0.0f;

        internal InputDeviceHandle _deviceHandle;

        internal InputStream() {}
    }
}
