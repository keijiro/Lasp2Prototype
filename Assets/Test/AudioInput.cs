using UnityEngine;

public sealed class AudioInput : MonoBehaviour
{
    [SerializeField] bool _useDefaultDevice = true;
    [SerializeField] string _deviceID = "";
    [SerializeField] int _channel = 0;

    Lasp.InputStream _stream;
    Lasp.InputStream Stream => GetAndCacheStream();

    Lasp.InputStream GetAndCacheStream()
    {
        if (_stream == null || !_stream.IsValid)
            _stream = _useDefaultDevice ?
              Lasp.AudioSystem.GetDefaultInputStream() :
              Lasp.AudioSystem.GetInputStream(_deviceID);
        return _stream;
    }

    void Update()
    {
        var level = Stream?.GetChannelLevel(_channel) ?? 0;
        transform.localScale = Vector3.one * level * 10;
    }
}
