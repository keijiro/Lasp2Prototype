using UnityEngine;

public sealed class AudioInput : MonoBehaviour
{
    [SerializeField] bool _useDefaultDevice = true;
    [SerializeField] string _deviceID = "";
    [SerializeField] int _channel = 0;

    Lasp.InputStream _stream;

    void Start()
    {
        _stream = _useDefaultDevice ?
            Lasp.DeviceManager.GetDefaultInputStream() :
            Lasp.DeviceManager.GetInputStream(_deviceID);
    }

    void Update()
    {
        transform.localScale = Vector3.one * _stream.AudioRmsLevel * 10;
    }
}
