using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Lasp.Editor
{
    sealed class ComponentSelector
    {
        string [] _componentNames;
        GameObject _cachedGameObject;

        public void ShowUI(SerializedProperty sp)
        {
            // Target component field edit
            EditorGUILayout.PropertyField(sp);

            // Show the following controls only when the component exists.
            var component = sp.objectReferenceValue as Component;
            if (component == null) return;

            // Cache the component condidates.
            var gameObject = component.gameObject;
            if (gameObject != _cachedGameObject)
            {
                _componentNames = gameObject.GetComponents<Component>()
                                  .Select(x => x.GetType().Name).ToArray();
                _cachedGameObject = gameObject;
            }

            // Current selection
            var index = System.Array.IndexOf
                        (_componentNames, component.GetType().Name);

            // Component selection drop down
            EditorGUI.BeginChangeCheck();
            index = EditorGUILayout.Popup("Component", index, _componentNames);
            if (EditorGUI.EndChangeCheck())
                sp.objectReferenceValue =
                    gameObject.GetComponent(_componentNames[index]);
        }
    }
}
