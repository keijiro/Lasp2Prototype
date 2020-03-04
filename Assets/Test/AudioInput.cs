using UnityEngine;

public sealed class AudioInput : MonoBehaviour
{
    [SerializeField] bool _useDefaultDevice = true;
    [SerializeField] string _deviceID = "";
    [SerializeField] int _channel = 0;

    Lasp.InputStream _stream;

    void Start()
    {
        _stream = Lasp.DeviceManager.GetInputStream(_deviceID);
    }

    void Update()
    {
        var sq_sum = 0.0f;
        foreach (var v in _stream.AudioDataSpan) sq_sum += v * v;
        Debug.Log(Mathf.Sqrt(sq_sum / _stream.AudioDataSpan.Length));
    }

    public void NotInUse()
    {
        Debug.Log($"{_useDefaultDevice} {_deviceID} {_channel}");
    }
}
