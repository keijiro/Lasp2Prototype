using UnityEngine;
using UnityEditor;

namespace Lasp.Editor
{
    [CustomEditor(typeof(PropertyBinderTest))]
    sealed class PropertyBinderTestEditor : UnityEditor.Editor
    {
        #region Inspector implementation

        SerializedProperty _binders;

        static class Styles
        {
            public static Label Value0 = "Value at 0";
            public static Label Value1 = "Value at 1";
        }

        void OnEnable()
        {
            var finder = new PropertyFinder(serializedObject);
            _binders = finder["_binders"];
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            for (var i = 0; i < _binders.arraySize; i++)
            {
                CoreEditorUtils.DrawSplitter();
                ShowPropertyBinder(_binders.GetArrayElementAtIndex(i));
            }

            serializedObject.ApplyModifiedProperties();

            CoreEditorUtils.DrawSplitter();

            // "Add Property Binder" button
            var rect = EditorGUILayout.GetControlRect();
            rect.x += (rect.width - 200) / 2;
            rect.width = 200;

            if (GUI.Button(rect, "Add Property Binder"))
                CreateNewPropertyBinderMenu().DropDown(rect);
        }

        #endregion

        #region Methods for property binders

        void ShowPropertyBinder(SerializedProperty prop)
        {
            var finder = new RelativePropertyFinder(prop);

            var toggle = CoreEditorUtils.DrawHeaderToggle
              (prop.managedReferenceFullTypename, prop, finder["_enabled"], pos => {});

            if (!toggle) return;

            EditorGUILayout.PropertyField(finder["_target"]);
            EditorGUILayout.PropertyField(finder["_propertyName"]);
            EditorGUILayout.PropertyField(finder["_value0"], Styles.Value0);
            EditorGUILayout.PropertyField(finder["_value1"], Styles.Value1);
        }

        GenericMenu CreateNewPropertyBinderMenu()
        {
            var menu = new GenericMenu();
            AddPropertyBinderItem<        FloatPropertyBinder>(menu);
            AddPropertyBinderItem<      Vector3PropertyBinder>(menu);
            AddPropertyBinderItem<EulerRotationPropertyBinder>(menu);
            AddPropertyBinderItem<        ColorPropertyBinder>(menu);
            return menu;
        }

        void AddPropertyBinderItem<T>(GenericMenu menu)
        {
            var prettyName = ObjectNames.NicifyVariableName(typeof(T).Name);
            var shortName = prettyName.Replace("Property Binder", "");
            menu.AddItem(new GUIContent(shortName),
                         false, OnAddPropertyBinder, typeof(T));
        }

        void OnAddPropertyBinder(object type)
        {
            // Binder instance creation
            var binder = System.Activator.CreateInstance((System.Type)type);

            serializedObject.Update();

            // Add to the binder array.
            var i = _binders.arraySize;
            _binders.InsertArrayElementAtIndex(i);
            _binders.GetArrayElementAtIndex(i).managedReferenceValue = binder;

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }
}
