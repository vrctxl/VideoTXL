using UnityEditor;
using UnityEngine;

namespace Texel
{
    [CustomEditor(typeof(PlaylistQueue))]
    public class PlaylistQueueEditor : VideoUrlSourceInspector
    {
        protected SerializedProperty allowAddProperty;
        protected SerializedProperty addAccessProperty;
        protected SerializedProperty allowAddFromProxyProperty;

        protected SerializedProperty priorityAccessProperty;
        protected SerializedProperty allowPriorityProperty;
        protected SerializedProperty deleteAccessProperty;
        protected SerializedProperty allowDeleteProperty;
        protected SerializedProperty allowSelfDeleteProperty;

        protected SerializedProperty canInterruptSourcesProperty;
        protected SerializedProperty enableSyncQuestUrlsProperty;
        protected SerializedProperty syncTrackTitlesProperty;
        protected SerializedProperty syncTrackAuthorsProperty;
        protected SerializedProperty syncPlayerNamesProperty;

        protected override void OnEnable()
        {
            base.OnEnable();

            allowAddProperty = serializedObject.FindProperty(nameof(PlaylistQueue.allowAdd));
            addAccessProperty = serializedObject.FindProperty(nameof(PlaylistQueue.addAccess));
            allowAddFromProxyProperty = serializedObject.FindProperty(nameof(PlaylistQueue.allowAddFromProxy));

            priorityAccessProperty = serializedObject.FindProperty(nameof(PlaylistQueue.priorityAccess));
            allowPriorityProperty = serializedObject.FindProperty(nameof(PlaylistQueue.allowPriority));
            deleteAccessProperty = serializedObject.FindProperty(nameof(PlaylistQueue.deleteAccess));
            allowDeleteProperty = serializedObject.FindProperty(nameof(PlaylistQueue.allowDelete));
            allowSelfDeleteProperty = serializedObject.FindProperty(nameof(PlaylistQueue.allowSelfDelete));

            canInterruptSourcesProperty = serializedObject.FindProperty(nameof(PlaylistQueue.canInterruptSources));
            enableSyncQuestUrlsProperty = serializedObject.FindProperty(nameof(PlaylistQueue.enableSyncQuestUrls));
            syncTrackTitlesProperty = serializedObject.FindProperty(nameof(PlaylistQueue.syncTrackTitles));
            syncTrackAuthorsProperty = serializedObject.FindProperty(nameof(PlaylistQueue.syncTrackAuthors));
            syncPlayerNamesProperty = serializedObject.FindProperty(nameof(PlaylistQueue.syncPlayerNames));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            RenderUrlSourceInspector();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(canInterruptSourcesProperty, new GUIContent("Can Interrupt Sources", "When enabled, if the currently playing URL source is interruptible, then adding a track will interrupt that source's playback."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Permissions", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(allowAddProperty, new GUIContent("Allow Add", "Allow entries to be added to the queue."));
            if (allowAddProperty.boolValue)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.PropertyField(addAccessProperty, new GUIContent("Add Access", "Optional. ACL to control access to adding entries.  If not set, uses the video player's ACL settings."));
                EditorGUILayout.PropertyField(allowAddFromProxyProperty, new GUIContent("Allow Add From Proxy", "Allows entries to be added through the input proxy, regardless of overall access control restrictions.  This applies to external systems like YouTube Search Prefab."));
                EditorGUI.indentLevel -= 1;
            }

            EditorGUILayout.PropertyField(allowPriorityProperty, new GUIContent("Allow Priority", "Allows queue entries to be moved to the front."));
            if (allowPriorityProperty.boolValue)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.PropertyField(priorityAccessProperty, new GUIContent("Priority Access", "Optional. ACL to control access to the priority button.  If not set, uses the video player's ACL settings."));
                EditorGUI.indentLevel -= 1;
            }

            EditorGUILayout.PropertyField(allowDeleteProperty, new GUIContent("Allow Delete", "Allow added queue entries to be deleted."));
            if (allowDeleteProperty.boolValue)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.PropertyField(deleteAccessProperty, new GUIContent("Delete Access", "Optional. ACL to control access to the delete button.  If not set, uses the video player's ACL settings."));
                EditorGUILayout.PropertyField(allowSelfDeleteProperty, new GUIContent("Allow Self Delete", "Allows players to delete their own added entries, regardless of overall access control restrictions."));
                EditorGUI.indentLevel -= 1;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sync", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enableSyncQuestUrlsProperty, new GUIContent("Sync Quest URLs", "When enabled, supports tracking separate Quest URLs if they are added through the API."));
            EditorGUILayout.PropertyField(syncTrackTitlesProperty);
            EditorGUILayout.PropertyField(syncTrackAuthorsProperty);
            EditorGUILayout.PropertyField(syncPlayerNamesProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}