using UnityEngine;
using VRC.Udon;

using UnityEditor;
using UdonSharpEditor;
using System;

namespace Texel
{
    [CustomEditor(typeof(SourceManager))]
    public class SourceManagerInspector : Editor
    {
        SerializedProperty videoPlayerProperty;
        SerializedProperty sourcesProperty;

        DebugInspectorBlock debugBlock;

        private void OnEnable()
        {
            videoPlayerProperty = serializedObject.FindProperty(nameof(SourceManager.videoPlayer));
            sourcesProperty = serializedObject.FindProperty(nameof(SourceManager.sources));

            debugBlock = new DebugInspectorBlock(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            GUIStyle boldFoldoutStyle = new GUIStyle(EditorStyles.foldout);
            boldFoldoutStyle.fontStyle = FontStyle.Bold;

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(videoPlayerProperty, new GUIContent("Video Player"));
            EditorGUILayout.PropertyField(sourcesProperty, new GUIContent("Sources"));

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("+ Queue", "Adds a new Queue source to the end of the sources list."), GUILayout.Width(80)))
                VideoTxlManager.AddQueueToScene();

            GUILayout.Space(4);
            if (GUILayout.Button(new GUIContent("+ Playlist", "Adds a new Playlist source to the end of the sources list.\n\nThe playlist will include a new Playlist Data object."), GUILayout.Width(80)))
                VideoTxlManager.AddPlaylistToScene();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();
            debugBlock.Draw(TXLGUI.Styles.BoldFoldout);

            serializedObject.ApplyModifiedProperties();
        }
    }
}