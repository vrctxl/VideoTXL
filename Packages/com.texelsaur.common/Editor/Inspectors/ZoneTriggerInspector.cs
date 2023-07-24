
using UnityEditor;
using UdonSharpEditor;

namespace Texel
{
    [CustomEditor(typeof(ZoneTrigger))]
    public class ZoneTriggerInspector : Editor
    {
        SerializedProperty configureEventsProperty;
        SerializedProperty targetBehaviorProperty;
        SerializedProperty localPlayerOnlyProperty;
        SerializedProperty playerEnterEventProperty;
        SerializedProperty playerLeaveEventProperty;
        SerializedProperty playerTargetVariableProperty;

        private void OnEnable()
        {
            configureEventsProperty = serializedObject.FindProperty(nameof(CompoundZoneTrigger.configureEvents));
            targetBehaviorProperty = serializedObject.FindProperty(nameof(CompoundZoneTrigger.targetBehavior));
            localPlayerOnlyProperty = serializedObject.FindProperty(nameof(CompoundZoneTrigger.localPlayerOnly));
            playerEnterEventProperty = serializedObject.FindProperty(nameof(CompoundZoneTrigger.playerEnterEvent));
            playerLeaveEventProperty = serializedObject.FindProperty(nameof(CompoundZoneTrigger.playerLeaveEvent));
            playerTargetVariableProperty = serializedObject.FindProperty(nameof(CompoundZoneTrigger.playerTargetVariable));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(configureEventsProperty);
            if (configureEventsProperty.boolValue)
            {
                EditorGUILayout.PropertyField(targetBehaviorProperty);
                EditorGUILayout.PropertyField(playerEnterEventProperty);
                EditorGUILayout.PropertyField(playerLeaveEventProperty);
                EditorGUILayout.PropertyField(playerTargetVariableProperty);
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(localPlayerOnlyProperty);

            if (serializedObject.hasModifiedProperties)
                serializedObject.ApplyModifiedProperties();
        }
    }
}
