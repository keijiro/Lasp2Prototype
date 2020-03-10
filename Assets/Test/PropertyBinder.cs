using UnityEngine;
using UnityEngine.Events;

namespace Lasp
{
    [System.Serializable]
    public abstract class PropertyBinder
    {
        [SerializeField] protected Object _target = null;
        [SerializeField] protected string _propertyName = null;

        public float Level { set { OnSetLevel(value); } }

        public abstract void OnSetLevel(float level);

        protected UnityAction<T> GetPropertySetter<T>()
        {
            return (UnityAction<T>)System.Delegate.CreateDelegate
                (typeof(UnityAction<T>), _target, "set_" + _propertyName);
        }
    }

    public sealed class FloatPropertyBinder : PropertyBinder
    {
        [SerializeField] float _value0 = 0;
        [SerializeField] float _value1 = 1;

        UnityAction<float> Action
          => _action != null ? _action :
             (_action = GetPropertySetter<float>());

        UnityAction<float> _action;

        public override void OnSetLevel(float level)
          => Action(Mathf.Lerp(_value0, _value1, level));
    }

    public sealed class Vector3PropertyBinder : PropertyBinder
    {
        [SerializeField] Vector3 _value0 = Vector3.zero;
        [SerializeField] Vector3 _value1 = Vector3.one;

        UnityAction<Vector3> Action
          => _action != null ? _action :
             (_action = GetPropertySetter<Vector3>());

        UnityAction<Vector3> _action;

        public override void OnSetLevel(float level)
          => Action(Vector3.Lerp(_value0, _value1, level));
    }

    public sealed class EulerRotationPropertyBinder : PropertyBinder
    {
        [SerializeField] Vector3 _value0 = Vector3.zero;
        [SerializeField] Vector3 _value1 = new Vector3(0, 90, 0);

        UnityAction<Quaternion> Action
          => _action != null ? _action :
             (_action = GetPropertySetter<Quaternion>());

        UnityAction<Quaternion> _action;

        public override void OnSetLevel(float level)
          => Action(Quaternion.Euler(Vector3.Lerp(_value0, _value1, level)));
    }

    public sealed class ColorPropertyBinder : PropertyBinder
    {
        [SerializeField] Color _value0 = Color.black;
        [SerializeField] Color _value1 = Color.white;

        UnityAction<Color> Action
          => _action != null ? _action :
             (_action = GetPropertySetter<Color>());

        UnityAction<Color> _action;

        public override void OnSetLevel(float level)
          => Action(Color.Lerp(_value0, _value1, level));
    }
}
