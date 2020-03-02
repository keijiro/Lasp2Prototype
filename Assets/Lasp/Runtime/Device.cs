namespace Lasp
{
    public sealed class Device
    {
        public string ID { get; private set; }
        public string Name { get; private set; }

        internal Device(SoundIO.Device device)
        {
            ID = device.ID;
            Name = device.Name;
        }
    }
}
