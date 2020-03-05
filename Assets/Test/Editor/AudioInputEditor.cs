using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(AudioInput))]
public sealed class AudioInputEditor : Editor
{
    SerializedProperty _useDefaultDevice;
    SerializedProperty _deviceID;
    SerializedProperty _channel;

    static class Styles
    {
        public static GUIContent DefaultDevice = new GUIContent("Default Device");
        public static GUIContent Select = new GUIContent("Select");
    }

    void ShowDeviceSelectionDropdown(Rect rect)
    {
        var menu = new GenericMenu();
        var devices = Lasp.AudioSystem.InputDevices;

        if (devices.Any())
            foreach (var dev in devices)
                menu.AddItem(new GUIContent(dev.Name), false, OnSelectDevice, dev.ID);
        else
            menu.AddItem(new GUIContent("No device available"), false, null);

        menu.DropDown(rect);
    }

    void OnSelectDevice(object id)
    {
        serializedObject.Update();
        _deviceID.stringValue = (string)id;
        serializedObject.ApplyModifiedProperties();
    }

    void OnEnable()
    {
        _useDefaultDevice = serializedObject.FindProperty("_useDefaultDevice");
        _deviceID = serializedObject.FindProperty("_deviceID");
        _channel = serializedObject.FindProperty("_channel");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_useDefaultDevice, Styles.DefaultDevice);

        if (!_useDefaultDevice.boolValue || _useDefaultDevice.hasMultipleDifferentValues)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PropertyField(_deviceID);

            var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(60));
            if (EditorGUI.DropdownButton(rect, Styles.Select, FocusType.Keyboard))
                ShowDeviceSelectionDropdown(rect);

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.PropertyField(_channel);

        serializedObject.ApplyModifiedProperties();
    }
}
