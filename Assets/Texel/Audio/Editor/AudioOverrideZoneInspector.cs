using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using UnityEditorInternal;
using UdonSharpEditor;
using System;

namespace Texel
{
    [CustomEditor(typeof(AudioOverrideZone))]
    public class AudioOverrideZoneInspector : Editor
    {
        static bool _showLinkedListFoldout = true;
        static bool[] _showLinkedFoldout = new bool[0];

        SerializedProperty membershipProperty;
        SerializedProperty zoneTriggerProperty;

        SerializedProperty enableLocalProperty;
        SerializedProperty localSettingsProperty;

        SerializedProperty linkedZoneListProperty;
        SerializedProperty linkedSettingsListProperty;
        SerializedProperty linkedEnabledListProperty;

        SerializedProperty enableDefaultProperty;
        SerializedProperty defaultSettingsProperty;

        SerializedProperty debugLogProperty;
        SerializedProperty vrcLogProperty;

        private void OnEnable()
        {
            membershipProperty = serializedObject.FindProperty(nameof(AudioOverrideZone.membership));
            zoneTriggerProperty = serializedObject.FindProperty(nameof(AudioOverrideZone.zone));

            enableLocalProperty = serializedObject.FindProperty(nameof(AudioOverrideZone.localZoneEnabled));
            localSettingsProperty = serializedObject.FindProperty(nameof(AudioOverrideZone.localZoneSettings));

            linkedZoneListProperty = serializedObject.FindProperty(nameof(AudioOverrideZone.linkedZones));
            linkedSettingsListProperty = serializedObject.FindProperty(nameof(AudioOverrideZone.linkedZoneSettings));
            linkedEnabledListProperty = serializedObject.FindProperty(nameof(AudioOverrideZone.linkedZoneEnabled));

            enableDefaultProperty = serializedObject.FindProperty(nameof(AudioOverrideZone.defaultEnabled));
            defaultSettingsProperty = serializedObject.FindProperty(nameof(AudioOverrideZone.defaultSettings));

            debugLogProperty = serializedObject.FindProperty(nameof(AudioOverrideZone.debugLog));
            vrcLogProperty = serializedObject.FindProperty(nameof(AudioOverrideZone.vrcLogging));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(membershipProperty, new GUIContent("Membership", "ZoneMembership component to track membership of players within this zone.  Can be a component on this same object."));
            EditorGUILayout.PropertyField(zoneTriggerProperty, new GUIContent("Zone", "Zone trigger that defines volume of this override zone.  Can be a component on this same object with associated trigger collider."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Local Zone", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(localSettingsProperty, new GUIContent("Local Zone Settings", "An audio profile that will be applied against players standing in the same zone as the local player."));
            EditorGUILayout.PropertyField(enableLocalProperty, new GUIContent("Local Zone Enabled", "Whether the local settings profile should currently be used or not.  If disabled, zone will defer to the default profile instead."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Linked Zones", EditorStyles.boldLabel);
            ChannelFoldout();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Default Zone", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(defaultSettingsProperty, new GUIContent("Default Settings", "An audio profile that will be applied against any players that were not affected by the local zone or linked zone profiles."));
            EditorGUILayout.PropertyField(enableDefaultProperty, new GUIContent("Default Enabled", "Whether the default settings profile should currently be used or not.  If disabled, zone will defer to the system-wide default profile isntead."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Logging", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(debugLogProperty, new GUIContent("Debug Log", "Debug log object to write out debug info to."));
            EditorGUILayout.PropertyField(vrcLogProperty, new GUIContent("VRC Log", "Whethre debug info should be written to the VRC log."));


            serializedObject.ApplyModifiedProperties();
        }

        private void ChannelFoldout()
        {
            int count = linkedZoneListProperty.arraySize;

            _showLinkedListFoldout = EditorGUILayout.Foldout(_showLinkedListFoldout, $"Linked Zones ({count})");
            if (!_showLinkedListFoldout)
                return;

            EditorGUI.indentLevel++;
            _showLinkedFoldout = EditorTools.MultiArraySize(serializedObject, _showLinkedFoldout,
                linkedZoneListProperty, linkedSettingsListProperty, linkedEnabledListProperty);

            for (int i = 0; i < linkedZoneListProperty.arraySize; i++)
            {
                string name = EditorTools.GetObjectName(linkedZoneListProperty, i);
                _showLinkedFoldout[i] = EditorGUILayout.Foldout(_showLinkedFoldout[i], $"Linked Zone {i} ({name})");
                if (!_showLinkedFoldout[i])
                    continue;

                EditorGUI.indentLevel++;

                SerializedProperty zone = linkedZoneListProperty.GetArrayElementAtIndex(i);
                SerializedProperty settings = linkedSettingsListProperty.GetArrayElementAtIndex(i);
                SerializedProperty enabled = linkedEnabledListProperty.GetArrayElementAtIndex(i);

                EditorGUILayout.PropertyField(zone, new GUIContent("Override Zone", "An audio override zone that should affect this zone."));
                EditorGUILayout.PropertyField(settings, new GUIContent("Zone Settings", "An audio profile that should be applied to players standing in the remote override zone."));
                EditorGUILayout.PropertyField(enabled, new GUIContent("Enabled", "Whether the link between zones should currently be used or not.  If disabled, zone will defer to the default profile instead."));

                if (GUILayout.Button("Remove", GUILayout.Width(EditorGUIUtility.labelWidth)))
                {
                    RemoveIndex(i);
                    i--;
                }
                EditorGUILayout.Space();

                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }

        private void RemoveIndex(int index)
        {
            if (index < 0 || index >= linkedZoneListProperty.arraySize)
                return;

            bool[] foldout = new bool[linkedZoneListProperty.arraySize - 1];
            Array.Copy(_showLinkedFoldout, 0, foldout, 0, index);
            Array.Copy(_showLinkedFoldout, index, foldout, index - 1, linkedZoneListProperty.arraySize - index - 1);
            _showLinkedFoldout = foldout;

            linkedZoneListProperty.GetArrayElementAtIndex(index).objectReferenceValue = null;
            linkedSettingsListProperty.GetArrayElementAtIndex(index).objectReferenceValue = null;

            linkedZoneListProperty.DeleteArrayElementAtIndex(index);
            linkedSettingsListProperty.DeleteArrayElementAtIndex(index);
            linkedEnabledListProperty.DeleteArrayElementAtIndex(index);
        }
    }
}