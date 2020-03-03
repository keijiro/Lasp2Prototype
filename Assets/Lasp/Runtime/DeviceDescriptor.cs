namespace Lasp
{
    public struct DeviceDescriptor
    {
        public string ID => _handle.SioDevice.ID;
        public string Name => _handle.SioDevice.Name;

        internal InputDeviceHandle _handle;
    }
}
