using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using UnityEditorInternal;
using UdonSharpEditor;

namespace Texel
{
    [CustomEditor(typeof(CompoundZoneTrigger))]
    public class CompoundZoneTriggerInspector : Editor
    {
        SerializedProperty configureEventsProperty;
        SerializedProperty targetBehaviorProperty;
        SerializedProperty localPlayerOnlyProperty;
        SerializedProperty playerEnterEventProperty;
        SerializedProperty playerLeaveEventProperty;
        SerializedProperty playerTargetVariableProperty;
        SerializedProperty enterSetModeProperty;
        SerializedProperty leaveSetModeProperty;
        SerializedProperty latchUntilEnterProperty;
        SerializedProperty latchUntilLeaveProperty;
        SerializedProperty recalcCollidersOnStartProperty;
        SerializedProperty forceColliderCheckProperty;
        SerializedProperty debugLogProperty;
        SerializedProperty vrcLogProperty;

        private void OnEnable()
        {
            configureEventsProperty = serializedObject.FindProperty(nameof(CompoundZoneTrigger.configureEvents));
            targetBehaviorProperty = serializedObject.FindProperty(nameof(CompoundZoneTrigger.targetBehavior));
            localPlayerOnlyProperty = serializedObject.FindProperty(nameof(CompoundZoneTrigger.localPlayerOnly));
            playerEnterEventProperty = serializedObject.FindProperty(nameof(CompoundZoneTrigger.playerEnterEvent));
            playerLeaveEventProperty = serializedObject.FindProperty(nameof(CompoundZoneTrigger.playerLeaveEvent));
            playerTargetVariableProperty = serializedObject.FindProperty(nameof(CompoundZoneTrigger.playerTargetVariable));
            enterSetModeProperty = serializedObject.FindProperty(nameof(CompoundZoneTrigger.enterSetMode));
            leaveSetModeProperty = serializedObject.FindProperty(nameof(CompoundZoneTrigger.leaveSetMode));
            latchUntilEnterProperty = serializedObject.FindProperty(nameof(CompoundZoneTrigger.latchUntilEnter));
            latchUntilLeaveProperty = serializedObject.FindProperty(nameof(CompoundZoneTrigger.latchUntilLeave));
            recalcCollidersOnStartProperty = serializedObject.FindProperty(nameof(CompoundZoneTrigger.recalcCollidersOnStart));
            forceColliderCheckProperty = serializedObject.FindProperty(nameof(CompoundZoneTrigger.forceColliderCheck));
            debugLogProperty = serializedObject.FindProperty(nameof(CompoundZoneTrigger.debugLog));
            vrcLogProperty = serializedObject.FindProperty(nameof(CompoundZoneTrigger.vrcLog));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(configureEventsProperty);
            if (configureEventsProperty.boolValue)
                EditorGUILayout.PropertyField(targetBehaviorProperty);
            EditorGUILayout.PropertyField(localPlayerOnlyProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Enter Trigger", EditorStyles.boldLabel);
            if (localPlayerOnlyProperty.boolValue)
            {
                GUIContent enterDesc = new GUIContent(enterSetModeProperty.displayName, "How multiple colliders should be treated for triggering an enter event");
                enterSetModeProperty.intValue = EditorGUILayout.Popup(enterDesc, enterSetModeProperty.intValue, new string[] { "Independent", "Union", "Intersection" });
                if (enterSetModeProperty.intValue == CompoundZoneTrigger.SET_INTERSECT)
                    EditorGUILayout.PropertyField(latchUntilLeaveProperty);
            }
            if (configureEventsProperty.boolValue)
                EditorGUILayout.PropertyField(playerEnterEventProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Leave Trigger", EditorStyles.boldLabel);
            if (localPlayerOnlyProperty.boolValue)
            {
                GUIContent leaveDesc = new GUIContent(leaveSetModeProperty.displayName, "How multiple colliders should be treated for triggering an leave event");
                leaveSetModeProperty.intValue = EditorGUILayout.Popup(leaveDesc, leaveSetModeProperty.intValue, new string[] { "Independent", "Union", "Intersection" });
                if (leaveSetModeProperty.intValue == CompoundZoneTrigger.SET_UNION)
                    EditorGUILayout.PropertyField(latchUntilEnterProperty);
            }
            if (configureEventsProperty.boolValue)
                EditorGUILayout.PropertyField(playerLeaveEventProperty);

            if (configureEventsProperty.boolValue)
                EditorGUILayout.PropertyField(playerTargetVariableProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Extra", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(forceColliderCheckProperty);
            EditorGUILayout.PropertyField(recalcCollidersOnStartProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(debugLogProperty);
            EditorGUILayout.PropertyField(vrcLogProperty);

            if (serializedObject.hasModifiedProperties)
                serializedObject.ApplyModifiedProperties();
        }
    }
}