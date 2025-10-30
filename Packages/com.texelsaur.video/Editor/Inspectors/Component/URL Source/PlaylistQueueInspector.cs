using UnityEditor;
using UnityEngine;

namespace Texel
{
    [CustomEditor(typeof(PlaylistQueue))]
    public class PlaylistQueueEditor : VideoUrlSourceInspector
    {
        protected SerializedProperty priorityAccessProperty;
        protected SerializedProperty deleteAccessProperty;

        protected SerializedProperty syncTrackTitlesProperty;
        protected SerializedProperty syncTrackAuthorsProperty;
        protected SerializedProperty syncPlayerNamesProperty;

        protected override void OnEnable()
        {
            base.OnEnable();

            priorityAccessProperty = serializedObject.FindProperty(nameof(PlaylistQueue.priorityAccess));
            deleteAccessProperty = serializedObject.FindProperty(nameof(PlaylistQueue.deleteAccess));

            syncTrackTitlesProperty = serializedObject.FindProperty(nameof(PlaylistQueue.syncTrackTitles));
            syncTrackAuthorsProperty = serializedObject.FindProperty(nameof(PlaylistQueue.syncTrackAuthors));
            syncPlayerNamesProperty = serializedObject.FindProperty(nameof(PlaylistQueue.syncPlayerNames));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            RenderUrlSourceInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Permissions", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(priorityAccessProperty);
            EditorGUILayout.PropertyField(deleteAccessProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sync", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(syncTrackTitlesProperty);
            EditorGUILayout.PropertyField(syncTrackAuthorsProperty);
            EditorGUILayout.PropertyField(syncPlayerNamesProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}