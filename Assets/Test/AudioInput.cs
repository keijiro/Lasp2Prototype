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
        var data = _stream.AudioDataSpan;
        var stride = _stream.ChannelCount;

        var sq_sum = 0.0f;
        for (var i = _channel; i < data.Length; i += stride)
            sq_sum += data[i] * data[i];
        var rms = Mathf.Sqrt(sq_sum / data.Length);

        Debug.Log($"RMS = {rms}");
    }
}
