
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEditorInternal;
using UdonSharpEditor;
#endif

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class PlaylistData : UdonSharpBehaviour
    {
        public string playlistName;

        public VRCUrl[] playlist;
        public string[] trackNames;
    }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(PlaylistData))]
    internal class PlaylistDataInspector : Editor
    {
        static bool _showPlaylistFoldout = true;
        static bool[] _showEntryFoldout = new bool[0];

        SerializedProperty playlistNameProperty;

        SerializedProperty playlistProperty;
        SerializedProperty trackNamesProperty;

        private void OnEnable()
        {
            playlistNameProperty = serializedObject.FindProperty(nameof(PlaylistData.playlistName));

            playlistProperty = serializedObject.FindProperty(nameof(PlaylistData.playlist));
            trackNamesProperty = serializedObject.FindProperty(nameof(PlaylistData.trackNames));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            OverrideFoldout();

            serializedObject.ApplyModifiedProperties();
        }

        private void OverrideFoldout()
        {
            EditorGUILayout.PropertyField(playlistNameProperty);
            EditorGUILayout.Space();

            _showPlaylistFoldout = EditorGUILayout.Foldout(_showPlaylistFoldout, "Playlist Entries");
            if (_showPlaylistFoldout)
            {
                EditorGUI.indentLevel++;
                int newCount = Mathf.Max(0, EditorGUILayout.DelayedIntField("Size", playlistProperty.arraySize));
                if (newCount != playlistProperty.arraySize)
                    playlistProperty.arraySize = newCount;
                if (newCount != trackNamesProperty.arraySize)
                    trackNamesProperty.arraySize = newCount;

                if (_showEntryFoldout.Length != playlistProperty.arraySize)
                {
                    _showEntryFoldout = new bool[playlistProperty.arraySize];
                    for (int i = 0; i < _showEntryFoldout.Length; i++)
                        _showEntryFoldout[i] = true;
                }

                for (int i = 0; i < playlistProperty.arraySize; i++)
                {
                    _showEntryFoldout[i] = EditorGUILayout.Foldout(_showEntryFoldout[i], "Track " + i);
                    if (_showEntryFoldout[i])
                    {
                        EditorGUI.indentLevel++;

                        SerializedProperty url = playlistProperty.GetArrayElementAtIndex(i);
                        SerializedProperty name = trackNamesProperty.GetArrayElementAtIndex(i);

                        EditorGUILayout.PropertyField(url, new GUIContent("URL"));
                        EditorGUILayout.PropertyField(name, new GUIContent("Track Name"));

                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }
        }
    }
#endif
}
