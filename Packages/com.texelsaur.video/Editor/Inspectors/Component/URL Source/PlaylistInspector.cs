using UnityEditor;
using UnityEngine;

namespace Texel
{
    [CustomEditor(typeof(Playlist))]
    public class PlaylistInspector : VideoUrlSourceInspector
    {
        protected SerializedProperty shuffleProperty;
        protected SerializedProperty autoAdvanceProperty;
        protected SerializedProperty trackCatalogModeProperty;
        protected SerializedProperty immediateProperty;
        protected SerializedProperty resumeAfterLoadProperty;

        protected SerializedProperty playlistCatalogProperty;
        protected SerializedProperty playlistDataProperty;
        protected SerializedProperty queueProperty;

        protected SerializedProperty debugLogProperty;

        protected override void OnEnable()
        {
            base.OnEnable();

            shuffleProperty = serializedObject.FindProperty(nameof(Playlist.shuffle));
            autoAdvanceProperty = serializedObject.FindProperty(nameof(Playlist.autoAdvance));
            trackCatalogModeProperty = serializedObject.FindProperty(nameof(Playlist.trackCatalogMode));
            immediateProperty = serializedObject.FindProperty(nameof(Playlist.immediate));
            resumeAfterLoadProperty = serializedObject.FindProperty(nameof(Playlist.resumeAfterLoad));
            playlistCatalogProperty = serializedObject.FindProperty(nameof(Playlist.playlistCatalog));
            playlistDataProperty = serializedObject.FindProperty(nameof(Playlist.playlistData));
            queueProperty = serializedObject.FindProperty(nameof(Playlist.queue));
            debugLogProperty = serializedObject.FindProperty(nameof(Playlist.debugLog));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw shared base fields
            RenderUrlSourceInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(shuffleProperty);
            EditorGUILayout.PropertyField(autoAdvanceProperty);
            EditorGUILayout.PropertyField(trackCatalogModeProperty);
            EditorGUILayout.PropertyField(immediateProperty);
            EditorGUILayout.PropertyField(resumeAfterLoadProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Data", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(playlistCatalogProperty);
            EditorGUILayout.PropertyField(playlistDataProperty);
            EditorGUILayout.PropertyField(queueProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(debugLogProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}