using UnityEngine;
using UnityEditor;

namespace Lasp.Editor
{
    [CustomEditor(typeof(PropertyBinderTest))]
    sealed class PropertyBinderTestEditor : UnityEditor.Editor
    {
        #region Inspector implementation

        SerializedProperty _actions;

        static class Styles
        {
            public static Label Value0 = "Value at 0";
            public static Label Value1 = "Value at 1";
        }

        void OnEnable()
        {
            var finder = new PropertyFinder(serializedObject);
            _actions = finder["_binders"];
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            for (var i = 0; i < _actions.arraySize; i++)
            {
                CoreEditorUtils.DrawSplitter();

                var element = _actions.GetArrayElementAtIndex(i);
                var typename = element.managedReferenceFullTypename;
                var finder = new RelativePropertyFinder(element);

                EditorGUILayout.PropertyField(finder["_target"]);
                EditorGUILayout.PropertyField(finder["_propertyName"]);
                EditorGUILayout.PropertyField(finder["_value0"], Styles.Value0);
                EditorGUILayout.PropertyField(finder["_value1"], Styles.Value1);
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
