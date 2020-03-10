using UnityEngine;
using UnityEditor;

namespace Lasp.Editor
{
    [CustomEditor(typeof(ActionTest))]
    sealed class AudioActionEditor : UnityEditor.Editor
    {
        #region Inspector implementation

        SerializedProperty _actions;

        void OnEnable()
        {
            _actions = serializedObject.FindProperty("_binders");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            for (var i = 0; i < _actions.arraySize; i++)
            {
                CoreEditorUtils.DrawSplitter();

                var element = _actions.GetArrayElementAtIndex(i);
                var typename = element.managedReferenceFullTypename;

                EditorGUILayout.PropertyField(element.FindPropertyRelative("_target"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("_propertyName"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("_value0"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("_value1"));
            }

            serializedObject.ApplyModifiedProperties();

            CoreEditorUtils.DrawSplitter();

            // Button rect
            var rect = EditorGUILayout.GetControlRect();
            rect.x += (rect.width - 200) / 2;
            rect.width = 200;

            // Property binder button
            if (GUI.Button(rect, "Add Property Binder"))
            {
                var menu = new GenericMenu();
                NewPropertyBinderItem<FloatPropertyBinder>(menu);
                NewPropertyBinderItem<Vector3PropertyBinder>(menu);
                NewPropertyBinderItem<EulerRotationPropertyBinder>(menu);
                NewPropertyBinderItem<ColorPropertyBinder>(menu);
                menu.DropDown(rect);
            }
        }

        #endregion

        #region Property binder menu

        void NewPropertyBinderItem<T>(GenericMenu menu)
        {
            var prettyName = ObjectNames.NicifyVariableName(typeof(T).Name);
            var shortName = prettyName.Replace("Property Binder", "");
            var label = new GUIContent(shortName);
            menu.AddItem(label, false, OnAddPropertyBinder, typeof(T));
        }

        void OnAddPropertyBinder(object type)
        {
            var binder = System.Activator.CreateInstance((System.Type)type);

            serializedObject.Update();

            var i = _actions.arraySize;
            _actions.InsertArrayElementAtIndex(i);
            _actions.GetArrayElementAtIndex(i).managedReferenceValue = binder;

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }
}
