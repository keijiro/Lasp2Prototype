using UnityEngine;

public sealed class AudioInput : MonoBehaviour
{
    [SerializeField] bool _useDefaultDevice = true;
    [SerializeField] string _deviceID = "";
    [SerializeField] int _channel = 0;

    public void NotInUse()
    {
        Debug.Log($"{_useDefaultDevice} {_deviceID} {_channel}");
    }
}
