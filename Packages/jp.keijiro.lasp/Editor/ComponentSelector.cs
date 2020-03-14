using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

namespace Lasp.Editor
{
    sealed class ComponentSelector
    {
        public bool ShowUI(SerializedProperty spTarget)
        {
            // Target component field
            EditorGUILayout.PropertyField(spTarget);

            // Show the following controls only when the component exists.
            var component = spTarget.objectReferenceValue as Component;
            if (component == null) return false;

            // Candidate enumeration
            var gameObject = component.gameObject;
            if (gameObject != _cachedGameObject)
            {
                _candidates = gameObject.GetComponents<Component>()
                  .Select(x => x.GetType().Name).ToArray();
                _cachedGameObject = gameObject;
            }

            // Current selection
            var index = Array.IndexOf(_candidates, component.GetType().Name);

            // Component selection drop down
            EditorGUI.BeginChangeCheck();
            index = EditorGUILayout.Popup("Component", index, _candidates);
            if (EditorGUI.EndChangeCheck())
                spTarget.objectReferenceValue =
                  gameObject.GetComponent(_candidates[index]);

            return true;
        }

        GameObject _cachedGameObject;
        string [] _candidates;
    }
}
