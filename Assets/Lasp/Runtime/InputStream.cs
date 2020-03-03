using System;
using Unity.Collections;

namespace Lasp
{
    public enum FilterType { Bypass, LowPass, BandPass, HighPass }

    public sealed class InputStream
    {
        public DeviceDescriptor Device { get; private set; }
        public FilterType FilterType { get; set; }

        public ReadOnlySpan<float> AudioDataSpan => ReadOnlySpan<float>.Empty;
        public NativeSlice<float> AudioDataSlice => default(NativeSlice<float>);

        public float AudioRmsLevel => 0.0f;

        internal InputDeviceHandle _deviceHandle;

        internal InputStream() {}
    }
}
