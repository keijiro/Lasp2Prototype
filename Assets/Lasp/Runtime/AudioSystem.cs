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
          => InputDeviceList.
             Select(dev => new DeviceDescriptor { _handle = dev });

        public static DeviceDescriptor FindInputDevice(string id)
          => new DeviceDescriptor
             { _handle = InputDeviceList.FirstOrDefault(dev => dev.ID == id) };

        public static InputStream GetInputStream(DeviceDescriptor desc)
          => InputStream.Create(desc._handle);

        public static InputStream GetDefaultInputStream()
          => InputStream.Create(InputDeviceList.First());

        public static InputStream GetInputStream(string id)
          => GetInputStream(FindInputDevice(id));

        #endregion

        #region Device list management

        static bool _shouldScanDevices = true;

        static InputDeviceList InputDeviceList => CheckAndGetInputDeviceList();
        static InputDeviceList _inputDeviceList = new InputDeviceList();

        static InputDeviceList CheckAndGetInputDeviceList()
        {
            Context.FlushEvents();
            if (_shouldScanDevices)
            {
                _inputDeviceList.ScanAvailable(Context);
                _shouldScanDevices = false;
            }
            return _inputDeviceList;
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
            foreach (var dev in _inputDeviceList) dev.Update(dt);
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
