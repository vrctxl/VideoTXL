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
        protected SerializedProperty interruptibleProperty;

        protected SerializedProperty playlistCatalogProperty;
        protected SerializedProperty playlistDataProperty;
        protected SerializedProperty queueProperty;

        protected SerializedProperty debugLogProperty;
        protected SerializedProperty debugLoggingProperty;

        protected override void OnEnable()
        {
            base.OnEnable();

            shuffleProperty = serializedObject.FindProperty(nameof(Playlist.shuffle));
            autoAdvanceProperty = serializedObject.FindProperty(nameof(Playlist.autoAdvance));
            trackCatalogModeProperty = serializedObject.FindProperty(nameof(Playlist.trackCatalogMode));
            immediateProperty = serializedObject.FindProperty(nameof(Playlist.immediate));
            resumeAfterLoadProperty = serializedObject.FindProperty(nameof(Playlist.resumeAfterLoad));
            interruptibleProperty = serializedObject.FindProperty(nameof(Playlist.interruptible));
            playlistCatalogProperty = serializedObject.FindProperty(nameof(Playlist.playlistCatalog));
            playlistDataProperty = serializedObject.FindProperty(nameof(Playlist.playlistData));
            queueProperty = serializedObject.FindProperty(nameof(Playlist.queue));
            debugLogProperty = serializedObject.FindProperty(nameof(Playlist.debugLog));
            debugLoggingProperty = serializedObject.FindProperty(nameof(Playlist.debugLogging));
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
            EditorGUILayout.PropertyField(interruptibleProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Data", EditorStyles.boldLabel);

            if (TXLGUI.DrawObjectFieldWithAdd(playlistCatalogProperty, new GUIContent("Playlist Catalog", "Optional catalog to sync a loaded playlist data across network."), new GUIContent("+", "Create new Playlist Catalog")))
                VideoTxlManager.AddPlaylistCatalogToScene(true);
            if (TXLGUI.DrawObjectFieldWithAdd(playlistDataProperty, new GUIContent("Playlist Data", "Default playlist track set."), new GUIContent("+", "Create new Playlist Data and add to catalog if present")))
                VideoTxlManager.AddPlaylistDataToScene(true);

            EditorGUILayout.PropertyField(queueProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(debugLogProperty);
            EditorGUILayout.PropertyField(debugLoggingProperty, new GUIContent("VRC Logging", "Write out video player events to VRChat log."));

            serializedObject.ApplyModifiedProperties();
        }
    }
}