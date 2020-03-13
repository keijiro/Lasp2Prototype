using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Lasp.Editor
{
    sealed class ComponentSelector
    {
        public void ShowUI(SerializedProperty sp)
        {
            EditorGUILayout.PropertyField(sp);

            CacheSiblingComponents(sp);

            // Component selection drop-down
            EditorGUI.BeginChangeCheck();

            var index = System.Array.IndexOf(_componentNames, sp.objectReferenceValue.GetType().Name);
            index = EditorGUILayout.Popup("Component", index, _componentNames);

            if (EditorGUI.EndChangeCheck())
                sp.objectReferenceValue =
                    ((Component)sp.objectReferenceValue).gameObject.GetComponent(_componentNames[index]);

         //   MoveRectToNextLine();
        }

        string [] _componentNames;
        GameObject _cachedGameObject;

        // Enumerate components in the same game object that the target
        // component is attached to.
        void CacheSiblingComponents(SerializedProperty sp)
        {
            var go = (sp.objectReferenceValue as Component)?.gameObject;
            if (_cachedGameObject == go) return;

            _componentNames = go.GetComponents<Component>().
                Select(x => x.GetType().Name).ToArray();

            _cachedGameObject = go;
        }
    }
}
