using System.Collections.Generic;
using System.Linq;
using UnityEngine.LowLevel;

namespace Lasp
{
    public static class DeviceManager
    {
        #region Device enumeration

        public static IEnumerable<DeviceDescriptor> InputDevices
            => EnumerateDeviceDescriptors();

        public static DeviceDescriptor FindDevice(string id)
            => InputDevices.FirstOrDefault(d => d.ID == id);

        public static InputStream GetInputStream(DeviceDescriptor desc)
            => new InputStream { _deviceHandle = desc._handle };

        public static InputStream GetInputStream(string id)
            => GetInputStream(FindDevice(id));

        static List<InputDeviceHandle> _inputDevices
            = new List<InputDeviceHandle>(); 

        static bool _shouldScanDevices = true;

        static IEnumerable<DeviceDescriptor> EnumerateDeviceDescriptors()
        {
            Context.FlushEvents();

            if (_shouldScanDevices)
            {
                // Reconstruct the device list.
                foreach (var dev in _inputDevices) dev.Dispose();
                _inputDevices.Clear();

                var count = Context.InputDeviceCount;
                for (var i = 0; i < count; i++)
                {
                    var dev = Context.GetInputDevice(i);
                    if (!dev.IsRaw && dev.Layouts.Length > 0)
                        _inputDevices.Add(InputDeviceHandle.CreateAndOwn(dev));
                    else
                        dev.Dispose();
                }

                _shouldScanDevices = false;
            }

            return _inputDevices.
                Select(dev => new DeviceDescriptor { _handle = dev });
        }

        #endregion

        static SoundIO.Context.OnDevicesChangeDelegate _onDevicesChangeDelegate =
            new SoundIO.Context.OnDevicesChangeDelegate(OnDevicesChange);

        static void OnDevicesChange(System.IntPtr pointer)
        {
            _shouldScanDevices = true;
        }

        #region SoundIO context management

        static SoundIO.Context Context => GetContextWithLazyInitialization();

        static SoundIO.Context _context;

        static SoundIO.Context GetContextWithLazyInitialization()
        {
            if (_context == null)
            {
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
                type = typeof(DeviceManager),
                updateDelegate = () => DeviceManager.Update()
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
