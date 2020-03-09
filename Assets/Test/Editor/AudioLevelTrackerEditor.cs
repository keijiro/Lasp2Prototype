using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Lasp
{
    //
    // Custom editor (inspector) for AudioLevelTracker
    //
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AudioLevelTracker))]
    sealed class AudioLevelTrackerEditor : Editor
    {
        SerializedProperty _useDefaultDevice;
        SerializedProperty _deviceID;
        SerializedProperty _channel;
        SerializedProperty _filterType;
        SerializedProperty _dynamicRange;
        SerializedProperty _autoGain;
        SerializedProperty _gain;
        SerializedProperty _holdAndFallDown;
        SerializedProperty _fallDownSpeed;
        SerializedProperty _normalizedLevelEvent;

        static class Styles
        {
            public static GUIContent NoDevice
              = new GUIContent("No device available");
            public static GUIContent DefaultDevice
              = new GUIContent("Default Device");
            public static GUIContent Select
              = new GUIContent("Select");
            public static GUIContent DynamicRange
              = new GUIContent("Dynamic Range (dB)");
            public static GUIContent Gain
              = new GUIContent("Gain (dB)");
            public static GUIContent Speed
              = new GUIContent("Speed");
        }

        // Device selection dropdown menu used for setting the device ID
        void ShowDeviceSelectionDropdown(Rect rect)
        {
            var menu = new GenericMenu();
            var devices = Lasp.AudioSystem.InputDevices;

            if (devices.Any())
                foreach (var dev in devices)
                    menu.AddItem(new GUIContent(dev.Name),
                                 false, OnSelectDevice, dev.ID);
            else
                menu.AddItem(Styles.NoDevice, false, null);

            menu.DropDown(rect);
        }

        // Device selection menu item callback
        void OnSelectDevice(object id)
        {
            serializedObject.Update();
            _deviceID.stringValue = (string)id;
            serializedObject.ApplyModifiedProperties();
        }

        void OnEnable()
        {
            _useDefaultDevice
              = serializedObject.FindProperty("_useDefaultDevice");
            _deviceID
              = serializedObject.FindProperty("_deviceID");
            _channel
              = serializedObject.FindProperty("_channel");
            _filterType
              = serializedObject.FindProperty("_filterType");
            _dynamicRange
              = serializedObject.FindProperty("_dynamicRange");
            _autoGain
              = serializedObject.FindProperty("_autoGain");
            _gain
              = serializedObject.FindProperty("_gain");
            _holdAndFallDown
              = serializedObject.FindProperty("_holdAndFallDown");
            _fallDownSpeed
              = serializedObject.FindProperty("_fallDownSpeed");
            _normalizedLevelEvent
              = serializedObject.FindProperty("_normalizedLevelEvent");
        }

        public override bool RequiresConstantRepaint()
        {
            // Keep updated while playing.
            return Application.isPlaying && targets.Length == 1;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Device selection
            EditorGUILayout.PropertyField(_useDefaultDevice, Styles.DefaultDevice);

            if (_useDefaultDevice.hasMultipleDifferentValues ||
                !_useDefaultDevice.boolValue)
            {
                // ID field and Select dropdown menu
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(_deviceID);
                var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(60));
                if (EditorGUI.DropdownButton(rect, Styles.Select, FocusType.Keyboard))
                    ShowDeviceSelectionDropdown(rect);
                EditorGUILayout.EndHorizontal();
            }

            // Input settings
            EditorGUILayout.PropertyField(_channel);
            EditorGUILayout.PropertyField(_filterType);
            EditorGUILayout.PropertyField(_dynamicRange, Styles.DynamicRange);
            EditorGUILayout.PropertyField(_autoGain);

            // Show Gain when no peak tracking.
            if (_autoGain.hasMultipleDifferentValues ||
                !_autoGain.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_gain, Styles.Gain);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(_holdAndFallDown);

            // Show Fall Down Speed when "Hold And Fall Down" is on.
            if (_holdAndFallDown.hasMultipleDifferentValues ||
                _holdAndFallDown.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_fallDownSpeed, Styles.Speed);
                EditorGUI.indentLevel--;
            }

            // Draw the level meter during play mode.
            if (RequiresConstantRepaint())
            {
                EditorGUILayout.Space();
                LevelMeterDrawer.DrawMeter((AudioLevelTracker)target);
            }

            // Show Reset Peak Level button during play mode.
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("Reset Auto Gain"))
                    foreach (AudioLevelTracker t in targets) t.ResetAutoGain();
            }

            EditorGUILayout.Space();

            // UnityEvent editor
            EditorGUILayout.PropertyField(_normalizedLevelEvent);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
