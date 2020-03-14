using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;

namespace Lasp.Editor
{
    sealed class PropertySelector
    {
        public bool ShowUI(SerializedProperty spTarget,
                           SerializedProperty spPropertyType,
                           SerializedProperty spPropertyName)
        {
            var componentType = spTarget.objectReferenceValue.GetType();
            var propertyTypeName = spPropertyType.stringValue;

            // Candidate enumeration
            if (componentType != _cachedComponentType ||
                propertyTypeName != _cachedPropertyTypeName)
            {
                // Determine the target property type using reflection.
                _cachedPropertyType = Type.GetType(propertyTypeName);

                // Property name candidates query
                _candidates = componentType
                  .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                  .Where(prop => prop.PropertyType == _cachedPropertyType)
                  .Select(prop => prop.Name).ToArray();

                _cachedComponentType = componentType;
                _cachedPropertyTypeName = propertyTypeName;
            }

            // Clear the selection and show a message if there is no candidate.
            if (_candidates.Length == 0)
            {
                EditorGUILayout.HelpBox
                  ($"No {_cachedPropertyType.Name} property found.",
                   MessageType.None);
                spPropertyName.stringValue = null;
                return false;
            }

            // Index of the current selection
            var index = Array.IndexOf(_candidates, spPropertyName.stringValue);

            // Drop down list
            EditorGUI.BeginChangeCheck();
            index = EditorGUILayout.Popup("Property", index, _candidates);
            if (EditorGUI.EndChangeCheck())
                spPropertyName.stringValue = _candidates[index];

            // Return true only when the selection is valid.
            return index >= 0;
        }

        Type _cachedComponentType;
        Type _cachedPropertyType;
        string _cachedPropertyTypeName;
        string [] _candidates;
    }
}
