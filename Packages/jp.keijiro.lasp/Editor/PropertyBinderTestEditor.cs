using UnityEngine;
using UnityEditor;

namespace Lasp.Editor
{
    [CustomEditor(typeof(PropertyBinderTest))]
    sealed class PropertyBinderTestEditor : UnityEditor.Editor
    {
        #region Inspector implementation

        SerializedProperty _binders;

        ComponentSelector _componentSelector = new ComponentSelector();

        static class Styles
        {
            public static Label Value0   = "Value at 0";
            public static Label Value1   = "Value at 1";
            public static Label MoveUp   = "Move Up";
            public static Label MoveDown = "Move Down";
            public static Label Remove   = "Remove";
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
                ShowPropertyBinderEditor(i);

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

        #region "Add Property Binder" button

        GenericMenu CreateNewPropertyBinderMenu()
        {
            var menu = new GenericMenu();
            AddNewPropertyBinderItem<        FloatPropertyBinder>(menu);
            AddNewPropertyBinderItem<      Vector3PropertyBinder>(menu);
            AddNewPropertyBinderItem<EulerRotationPropertyBinder>(menu);
            AddNewPropertyBinderItem<        ColorPropertyBinder>(menu);
            return menu;
        }

        void AddNewPropertyBinderItem<T>(GenericMenu menu) where T : new()
          => menu.AddItem(PropertyBinderTypeLabel<T>.Content,
                          false, OnAddNewPropertyBinder<T>);

        void OnAddNewPropertyBinder<T>() where T : new()
        {
            serializedObject.Update();

            var i = _binders.arraySize;
            _binders.InsertArrayElementAtIndex(i);
            _binders.GetArrayElementAtIndex(i).managedReferenceValue = new T();

            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region PropertyBinder editor

        void ShowPropertyBinderEditor(int index)
        {
            var prop = _binders.GetArrayElementAtIndex(index);
            var finder = new RelativePropertyFinder(prop);

            // Header
            CoreEditorUtils.DrawSplitter();

            var toggle = CoreEditorUtils.DrawHeaderToggle
              (PropertyBinderNameUtil.Shorten(prop),
               prop, finder["_enabled"],
               pos => CreateHeaderContextMenu(index)
                      .DropDown(new Rect(pos, Vector2.zero)));

            if (!toggle) return;

            // Properties
            //EditorGUILayout.PropertyField(finder["_target"]);
            _componentSelector.ShowUI(finder["_target"]);
            EditorGUILayout.PropertyField(finder["_propertyName"]);
            EditorGUILayout.PropertyField(finder["_value0"], Styles.Value0);
            EditorGUILayout.PropertyField(finder["_value1"], Styles.Value1);
        }

        #endregion

        #region ProeprtyBinder editor context menu

        GenericMenu CreateHeaderContextMenu(int index)
        {
            var menu = new GenericMenu();

            // Move up
            if (index == 0)
                menu.AddDisabledItem(Styles.MoveUp);
            else
                menu.AddItem(Styles.MoveUp, false,
                             () => OnMoveControl(index, index - 1));

            // Move down
            if (index == _binders.arraySize - 1)
                menu.AddDisabledItem(Styles.MoveDown);
            else
                menu.AddItem(Styles.MoveDown, false,
                             () => OnMoveControl(index, index + 1));

            menu.AddSeparator(string.Empty);

            // Remove
            menu.AddItem(Styles.Remove, false, () => OnRemoveControl(index));

            return menu;
        }

        void OnMoveControl(int src, int dst)
        {
            serializedObject.Update();
            _binders.MoveArrayElement(src, dst);
            serializedObject.ApplyModifiedProperties();
        }

        void OnRemoveControl(int index)
        {
            serializedObject.Update();
            _binders.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }
}
