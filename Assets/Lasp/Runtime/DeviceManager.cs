using System.Collections.Generic;
using System.Linq;
using UnityEngine.LowLevel;

namespace Lasp
{
    public static class DeviceManager
    {
        #region Device handling

        public static IEnumerable<Device> Devices => DeviceList;

        static List<Device> DeviceList => GetDevicesWithLazyInitialization();
        static List<Device> _deviceList;// = new List<Device>();

        static List<Device> GetDevicesWithLazyInitialization()
        {
            if (_shouldRescan)
            {
                _deviceList = null;
                _shouldRescan = false;
            }

            if (_deviceList != null) return _deviceList;

            _deviceList = new List<Device>();

            Context.FlushEvents();

            var count = Context.InputDeviceCount;
            for (var i = 0; i < count; i++)
            {
                using (var dev = Context.GetInputDevice(i))
                {
                    if (dev.IsRaw) continue;
                    if (dev.Layouts.Length == 0) continue;
                    _deviceList.Add(new Device(dev));
                }
            }

            UnityEngine.Debug.Log("SCAN");

            return _deviceList;
        }

        #endregion

        static SoundIO.Context.OnDevicesChangeDelegate _onDevicesChangeDelegate =
            new SoundIO.Context.OnDevicesChangeDelegate(OnDevicesChange);

        static bool _shouldRescan;

        static void OnDevicesChange(System.IntPtr pointer)
        {
            _shouldRescan = true;
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
