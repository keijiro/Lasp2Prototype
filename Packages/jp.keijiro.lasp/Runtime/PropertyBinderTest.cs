using UnityEngine;

namespace Lasp
{
    public sealed class PropertyBinderTest : MonoBehaviour
    {
        [SerializeReference] PropertyBinder [] _binders = null;

        void Update()
          { foreach (var a in _binders) a.Level = Time.time % 1.0f; }
    }
}
