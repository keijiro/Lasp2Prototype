using System.Collections.Generic;
using System.Linq;
using UnityEngine.LowLevel;

namespace Lasp
{
    //
    // Audio system class
    //
    // This class manages a global libsoundio context and a list of devices
    // found in the context. It's also in charge of invoking the Update
    // function of the device handle class using a Player Loop System.
    //
    public static class AudioSystem
    {
        #region Public members

        public static IEnumerable<DeviceDescriptor> InputDevices
          => EnumerateInputDevices();

        public static DeviceDescriptor FindDevice(string id)
          => InputDevices.FirstOrDefault(d => d.ID == id);

        public static InputStream GetInputStream(DeviceDescriptor desc)
          => InputStream.Create(desc._handle);

        public static InputStream GetDefaultInputStream()
          => InputStream.Create(InputDevices.First()._handle);

        public static InputStream GetInputStream(string id)
          => GetInputStream(FindDevice(id));

        #endregion

        #region Input device enumeration

        static List<InputDeviceHandle> _inputDevices
          = new List<InputDeviceHandle>(); 

        static bool _shouldScanDevices = true;

        // Scan and return descriptors of the input devices.
        static IEnumerable<DeviceDescriptor> EnumerateInputDevices()
        {
            Context.FlushEvents();

            if (_shouldScanDevices)
            {
                ScanInputDevices();
                _shouldScanDevices = false;
            }

            return _inputDevices.
              Select(dev => new DeviceDescriptor { _handle = dev });
        }

        // Scan and update the input device list.
        // It reuses object handles if their bound devices are still there.
        static void ScanInputDevices()
        {
            var deviceCount = Context.InputDeviceCount;
            var defaultIndex = Context.DefaultInputDeviceIndex;

            var founds = new List<InputDeviceHandle>();

            for (var i = 0; i < deviceCount; i++)
            {
                var dev = Context.GetInputDevice(i);

                // Check if the device is useful. Reject it if not.
                if (dev.IsRaw || dev.Layouts.Length < 1)
                {
                    dev.Dispose();
                    continue;
                }

                // Find the same device in the current list.
                var handle =
                  _inputDevices.FindAndRemove(h => h.SioDevice.ID == dev.ID);

                if (handle != null)
                {
                    // We reuse the handle, so this libsoundio device object
                    // should be disposed.
                    dev.Dispose();
                }
                else
                {
                    // Create a new handle with transferring the ownership of
                    // this libsoundio device object.
                    handle = InputDeviceHandle.CreateAndOwn(dev);
                }

                // Default device: Insert it at the head of the list.
                // Others: Simply append it to the list.
                if (i == defaultIndex)
                    founds.Insert(0, handle);
                else
                    founds.Add(handle);
            }

            // Dispose the remained handles (disconnected devices).
            foreach (var dev in _inputDevices) dev.Dispose();

            // Replace the list with the new one.
            _inputDevices = founds;
        }

        #endregion

        #region libsoundio context management

        static SoundIO.Context Context => GetContextWithLazyInitialization();

        static SoundIO.Context _context;

        static SoundIO.Context GetContextWithLazyInitialization()
        {
            if (_context == null)
            {
                // libsoundio context initialization
                _context = SoundIO.Context.Create();
                _context.OnDevicesChange = _onDevicesChangeDelegate;
                _context.Connect();
                _context.FlushEvents();

                // Install the Player Loop System.
                InsertPlayerLoopSystem();
            }

            return _context;
        }

        #endregion

        #region libsoundio context callback delegate

        static SoundIO.Context.OnDevicesChangeDelegate _onDevicesChangeDelegate
          = new SoundIO.Context.OnDevicesChangeDelegate(OnDevicesChange);

        static void OnDevicesChange(System.IntPtr pointer)
          => _shouldScanDevices = true;

        #endregion

        #region Update method implementation

        static void Update()
        {
            Context.FlushEvents();
            var dt = UnityEngine.Time.deltaTime;
            foreach (var dev in _inputDevices) dev.Update(dt);
        }

        #endregion

        #region PlayerLoopSystem implementation

        static void InsertPlayerLoopSystem()
        {
            // Append a custom system to the Early Update phase.

            var customSystem = new PlayerLoopSystem()
            {
                type = typeof(AudioSystem),
                updateDelegate = () => AudioSystem.Update()
            };

            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            for (var i = 0; i < playerLoop.subSystemList.Length; i++)
            {
                ref var phase = ref playerLoop.subSystemList[i];
                if (phase.type == typeof(UnityEngine.PlayerLoop.EarlyUpdate))
                {
                    phase.subSystemList = phase.subSystemList.
                        Concat(new[]{ customSystem }).ToArray();
                    break;
                }
            }

            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        #endregion
    }
}
