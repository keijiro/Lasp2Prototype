using UnityEngine;

namespace Lasp
{
    //
    // UnityEvent used to drive components by audio level
    //
    [System.Serializable]
    public class AudioLevelEvent : UnityEngine.Events.UnityEvent<float> {}

    //
    // Unity component used to track audio input level and drive other
    // components via UnityEvent
    //
    [AddComponentMenu("LASP/Audio Level Tracker")]
    public sealed class AudioLevelTracker : MonoBehaviour
    {
        #region Editor-only attributes

        // Device selection
        [SerializeField] bool _useDefaultDevice = true;
        [SerializeField] string _deviceID = "";

        #endregion

        #region Editor attributes and public properties

        // Channel Selection
        [SerializeField] int _channel = 0;
        public int channel
          { get => _channel;
            set => _channel = value; }

        // Filter type selection
        [SerializeField] FilterType _filterType = FilterType.Bypass;
        public FilterType filterType
          { get => _filterType;
            set => _filterType = value; }

        // Peak tracking (auto gain) switch
        [SerializeField] bool _peakTracking = true;
        public bool peakTracking
          { get => _peakTracking;
            set => _peakTracking = value; }

        // Manual gain (only used when peak tracking is off)
        [SerializeField, Range(-10, 40)] float _gain = 6;
        public float gain
          { get => _gain;
            set => _gain = value; }

        // Dynamic range in dB
        [SerializeField, Range(1, 40)] float _dynamicRange = 12;
        public float dynamicRange
          { get => _dynamicRange;
            set => _dynamicRange = value; }

        // "Hold and fall down" animation switch
        [SerializeField] bool _holdAndFallDown = true;
        public bool holdAndFallDown
          { get => _holdAndFallDown;
            set => _holdAndFallDown = value; }

        // Fall down animation speed
        [SerializeField, Range(0, 1)] float _fallDownSpeed = 0.3f;
        public float fallDownSpeed
          { get => _fallDownSpeed;
            set => _fallDownSpeed = value; }

        // Audio level (in normalized scale) output event
        [SerializeField] AudioLevelEvent _normalizedLevelEvent = null;
        public AudioLevelEvent normalizedLevelEvent
          { get => _normalizedLevelEvent;
            set => _normalizedLevelEvent = value; }

        #endregion

        #region Runtime public properties and methods

        // Current input gain value (dB)
        public float calculatedGain => _peakTracking ? -_peak : _gain;

        // Unprocessed amplitude value (dB)
        public float inputAmplitude
          => Stream?.GetChannelLevel(_channel, _filterType) ?? kSilence;

        // Curent level value in normalized scale
        public float normalizedLevel => _amplitude;

        // Reset the peak value used in auto gain.
        public void ResetPeak() => _peak = kSilence;

        #endregion

        #region Private members

        // Silence: Minimum amplitude value (dB)
        const float kSilence = -60;

        // Current amplitude value. (dB)
        float _amplitude = kSilence;

        // Variables usd in auto gain.
        float _peak = kSilence;
        float _fall = 0;

        // Input stream object with local cache
        InputStream Stream
          => (_stream != null && _stream.IsValid) ? _stream : CacheStream();

        InputStream CacheStream()
          => (_stream = _useDefaultDevice ?
               AudioSystem.GetDefaultInputStream() :
               AudioSystem.GetInputStream(_deviceID));

        InputStream _stream;

        #endregion

        #region MonoBehaviour implementation

        void Update()
        {
            var input = inputAmplitude;
            var dt = Time.deltaTime;

            // Automatic gain control
            if (_peakTracking)
            {
                // Gradually falls down to the minimum amplitude.
                const float peakFallSpeed = 0.6f;
                _peak = Mathf.Max(_peak - peakFallSpeed * dt, kSilence);

                // Pull up by input with allowing a small amount of clipping.
                var clip = _dynamicRange * 0.05f;
                _peak = Mathf.Clamp(input - clip, _peak, 0);
            }

            // Normalize the input value.
            input = Mathf.Clamp01((input + calculatedGain) / _dynamicRange + 1);

            if (_holdAndFallDown)
            {
                // Hold-and-fall-down animation.
                _fall += Mathf.Pow(10, 1 + _fallDownSpeed * 2) * dt;
                _amplitude -= _fall * dt;

                // Pull up by input.
                if (_amplitude < input)
                {
                    _amplitude = input;
                    _fall = 0;
                }
            }
            else
            {
                _amplitude = input;
            }

            // Output
            _normalizedLevelEvent?.Invoke(_amplitude);
        }

        #endregion
    }
}
