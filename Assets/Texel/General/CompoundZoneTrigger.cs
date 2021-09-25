
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEditorInternal;
using UdonSharpEditor;
#endif

namespace Texel
{
    [AddComponentMenu("Texel/General/Compound Zone Trigger")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class CompoundZoneTrigger : UdonSharpBehaviour
    {
        [Tooltip("If enabled, specify event handlers at edit time.  Handlers can still be registered at runtime.")]
        public bool configureEvents = false;
        [Tooltip("The Udon Behavior to send messages to on enter and leave events")]
        public UdonBehaviour targetBehavior;
        [Tooltip("Whether colliders should only recognize the local player")]
        public bool localPlayerOnly = true;
        [Tooltip("The event message to send on a player trigger enter event.  Leave blank to do nothing.")]
        public string playerEnterEvent;
        [Tooltip("The event message to send on a player trigger leave event.  Leave blank to do nothing.")]
        public string playerLeaveEvent;
        [Tooltip("Variable in remote script to write player reference before calling an enter or leave event.  Leave blank to not set player reference.")]
        public string playerTargetVariable;
        [Tooltip("How multiple colliders should be treated for triggering an enter event")]
        public int enterSetMode = SET_NONE;
        [Tooltip("How multiple colliders should be treated for triggering a leave event")]
        public int leaveSetMode = SET_NONE;
        [Tooltip("After sending an enter event, do not send another until leave has been triggered")]
        public bool latchUntilLeave;
        [Tooltip("After sending a leave event, do not send another until enter has been triggered")]
        public bool latchUntilEnter;

        int handlerCount = 0;

        [NonSerialized]
        public int colliderCount = 0;

        int triggerActiveCount = 0;
        bool enterLatched;
        bool leaveLatched;

        Collider[] colliders;

        Component[] targetBehaviors;
        string[] playerEnterEvents;
        string[] playerLeaveEvents;
        string[] playerTargetVariables;

        public const int SET_NONE = 0;
        public const int SET_UNION = 1;
        public const int SET_INTERSECT = 2;

        void Start()
        {
            if (configureEvents)
                _Register(targetBehavior, playerEnterEvent, playerLeaveEvent, playerTargetVariable);

            if (colliderCount == 0)
                _InitColliders();
        }

        public void _Register(UdonBehaviour target, string enterEvent, string leaveEvent, string targetVariable)
        {
            if (!Utilities.IsValid(target))
                return;

            if (!Utilities.IsValid(targetBehaviors))
            {
                targetBehaviors = new Component[0];
                playerEnterEvents = new string[0];
                playerLeaveEvents = new string[0];
                playerTargetVariables = new string[0];
            }

            if (enterEvent == "")
                enterEvent = null;
            if (leaveEvent == "")
                leaveEvent = null;
            if (targetVariable == "")
                targetVariable = null;

            // For shortcut case
            if (handlerCount == 0)
            {
                targetBehavior = target;
                playerEnterEvent = enterEvent;
                playerLeaveEvent = leaveEvent;
                playerTargetVariable = targetVariable;
            }

            int count = targetBehaviors.Length + 1;
            Component[] newTargetBehaviors = new Component[count];
            string[] newEnterEvents = new string[count];
            string[] newLeaveEvents = new string[count];
            string[] newVariables = new string[count];

            for (int i = 0; i < targetBehaviors.Length; i++)
            {
                newTargetBehaviors[i] = targetBehaviors[i];
                newEnterEvents[i] = playerEnterEvents[i];
                newLeaveEvents[i] = playerLeaveEvents[i];
                newVariables[i] = playerTargetVariables[i];
            }

            newTargetBehaviors[count - 1] = target;
            newEnterEvents[count - 1] = enterEvent;
            newLeaveEvents[count - 1] = leaveEvent;
            newVariables[count - 1] = targetVariable;

            targetBehaviors = newTargetBehaviors;
            playerEnterEvents = newEnterEvents;
            playerLeaveEvents = newLeaveEvents;
            playerTargetVariables = newVariables;

            handlerCount += 1;
        }

        void _InitColliders()
        {
            colliders = GetComponents<Collider>();
            foreach (var col in colliders)
            {
                if (Utilities.IsValid(col) && col.enabled)
                    colliderCount += 1;
            }
        }

        public void _PlayerTriggerReset()
        {
            triggerActiveCount = 0;
            enterLatched = false;
            leaveLatched = false;
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            _PlayerTriggerEnter(player);
        }

        public void _PlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!localPlayerOnly)
            {
                _SendPlayerEnter(player);
                return;
            }
            if (!player.isLocal)
                return;

            if (enterSetMode == SET_NONE)
                _SendPlayerEnter(player);

            if (enterSetMode == SET_UNION && triggerActiveCount == 0)
                _SendPlayerEnter(player);

            triggerActiveCount += 1;
            if (enterSetMode == SET_INTERSECT && triggerActiveCount == colliderCount)
            {
                if (!latchUntilLeave || !enterLatched)
                    _SendPlayerEnter(player);
                enterLatched = true;
            }
        }

        void _SendPlayerEvent(VRCPlayerApi player, UdonBehaviour target, string eventName, string varName)
        {
            if (eventName == null)
                return;

            if (varName != null)
                target.SetProgramVariable(varName, player);

            target.SendCustomEvent(eventName);
        }

        void _SendPlayerEnter(VRCPlayerApi player)
        {
            if (handlerCount == 1)
                _SendPlayerEvent(player, targetBehavior, playerEnterEvent, playerTargetVariable);
            else
            {
                for (int i = 0; i < handlerCount; i++)
                {
                    UdonBehaviour target = (UdonBehaviour)targetBehaviors[i];
                    _SendPlayerEvent(player, target, playerEnterEvents[i], playerTargetVariables[i]);
                }
            }

            leaveLatched = false;
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            _PlayerTriggerExit(player);
        }

        public void _PlayerTriggerExit(VRCPlayerApi player)
        {
            if (!localPlayerOnly)
            {
                _SendPlayerLeave(player);
                return;
            }
            if (!player.isLocal)
                return;

            if (leaveSetMode == SET_NONE)
                _SendPlayerLeave(player);

            if (leaveSetMode == SET_INTERSECT && triggerActiveCount == colliderCount)
            {
                if (!latchUntilEnter || !leaveLatched)
                    _SendPlayerLeave(player);
                leaveLatched = true;
            }

            triggerActiveCount -= 1;
            if (leaveSetMode == SET_UNION && triggerActiveCount == 0)
                _SendPlayerLeave(player);
        }

        void _SendPlayerLeave(VRCPlayerApi player)
        {
            if (handlerCount == 1)
                _SendPlayerEvent(player, targetBehavior, playerLeaveEvent, playerTargetVariable);
            else
            {
                for (int i = 0; i < handlerCount; i++)
                {
                    UdonBehaviour target = (UdonBehaviour)targetBehaviors[i];
                    _SendPlayerEvent(player, target, playerLeaveEvents[i], playerTargetVariables[i]);
                }
            }

            enterLatched = false;
        }

        public bool _LocalPlayerInZone()
        {
            if (!localPlayerOnly)
                return false;

            if (enterSetMode == SET_UNION)
                return triggerActiveCount > 0;
            if (enterSetMode == SET_INTERSECT)
                return triggerActiveCount == colliderCount || (latchUntilLeave && enterLatched);

            return false;
        }

        public void _LogEnter()
        {
            Debug.Log("Enter");
        }

        public void _LogLeave()
        {
            Debug.Log("Leave");
        }
    }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(CompoundZoneTrigger))]
    internal class ZoneTriggerInspector : Editor
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

            if (serializedObject.hasModifiedProperties)
                serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
