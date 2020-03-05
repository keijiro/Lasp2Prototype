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
            Lasp.AudioSystem.GetDefaultInputStream() :
            Lasp.AudioSystem.GetInputStream(_deviceID);
    }

    void Update()
    {
        if (_stream == null || !_stream.IsValid) return;
        var level = _stream.GetChannelLevel(_channel);
        transform.localScale = Vector3.one * level * 10;
    }
}
