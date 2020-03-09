using UnityEngine;

namespace Lasp
{
    // UnityEvent used to drive components with audio level
    [System.Serializable]
    public class AudioLevelEvent : UnityEngine.Events.UnityEvent<float> {}

    // Unity component used to track audio input level and drive other
    // components via UnityEvent
    [AddComponentMenu("LASP/Audio Level Tracker")]
    public sealed class AudioLevelTracker : MonoBehaviour
    {
        #region Editable attributes

        [SerializeField] bool _useDefaultDevice = true;
        [SerializeField] string _deviceID = "";
        [SerializeField] int _channel = 0;


        [SerializeField] FilterType _filterType = FilterType.Bypass;

        public Lasp.FilterType filterType {
            get { return _filterType; }
            set { _filterType = value; }
        }

        [SerializeField] bool _peakTracking = true;

        public bool peakTracking {
            get { return _peakTracking; }
            set { _peakTracking = value; }
        }

        [SerializeField, Range(-10, 40)] float _gain = 6;

        public float gain {
            get { return _gain; }
            set { _gain = value; }
        }

        [SerializeField, Range(1, 40)] float _dynamicRange = 12;

        public float dynamicRange {
            get { return _dynamicRange; }
            set { _dynamicRange = value; }
        }

        [SerializeField] bool _holdAndFallDown = true;

        public bool holdAndFallDown {
            get { return _holdAndFallDown; }
            set { _holdAndFallDown = value; }
        }

        [SerializeField, Range(0, 1)] float _fallDownSpeed = 0.3f;

        public float fallDownSpeed {
            get { return _fallDownSpeed; }
            set { _fallDownSpeed = value; }
        }

        [SerializeField]
        AudioLevelEvent _normalizedLevelEvent = new AudioLevelEvent();

        public AudioLevelEvent normalizedLevelEvent {
            get { return _normalizedLevelEvent; }
            set { _normalizedLevelEvent = value; }
        }

        #endregion

        #region Runtime public properties and methods

        public float calculatedGain {
            get { return _peakTracking ? -_peak : _gain; }
        }

        public float inputAmplitude {
            get { return Stream?.GetChannelLevel(_channel, _filterType) ?? kSilence; }
        }

        public float normalizedLevel {
            get { return _amplitude; }
        }

        public void ResetPeak()
        {
            _peak = kSilence;
        }

        #endregion

        #region Private members

        // Silence: Minimum amplitude value
        const float kSilence = -60;

        // Current amplitude value.
        float _amplitude = kSilence;

        // Variables for automatic gain control.
        float _peak = kSilence;
        float _fall = 0;

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
            _normalizedLevelEvent.Invoke(_amplitude);
        }

        #endregion
    }
}
