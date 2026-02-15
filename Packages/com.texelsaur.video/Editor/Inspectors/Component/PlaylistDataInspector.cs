
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System;
using System.Reflection;

using UnityEditor;
using UnityEditorInternal;
using UdonSharpEditor;

namespace Texel
{
    [Serializable]
    internal class PlaylistDataEntryJson
    {
        public string title;
        public string url;
        public string questUrl;
        public string urlForQuest;
    }

    [Serializable]
    internal class PlaylistDataJson
    {
        public string title;
        public PlaylistDataEntryJson[] entries;
    }

    [CustomEditor(typeof(PlaylistData))]
    internal class PlaylistDataInspector : Editor
    {
        static bool _showPlaylistFoldout = true;
        static bool[] _showEntryFoldout = new bool[0];

        SerializedProperty playlistNameProperty;

        SerializedProperty questFallbackTypeProperty;
        SerializedProperty questCustomPrefixProperty;

        SerializedProperty playlistProperty;
        SerializedProperty questPlaylistProperty;
        SerializedProperty trackNamesProperty;

        ReorderableList trackList;

        private string jsonInput = "{\n  \"title\": \"Playlist Name\",\n  \"entries\": [ {\n    \"title\": \"Track Name\",\n    \"url\": \"Track URL\",\n    \"questUrl\": \"Quest URL\"\n  } ]\n}";
        private Vector2 jsonScroll;

        private void OnEnable()
        {
            playlistNameProperty = serializedObject.FindProperty(nameof(PlaylistData.playlistName));

            questFallbackTypeProperty = serializedObject.FindProperty(nameof(PlaylistData.questFallbackType));
            questCustomPrefixProperty = serializedObject.FindProperty(nameof(PlaylistData.questCustomPrefix));

            playlistProperty = serializedObject.FindProperty(nameof(PlaylistData.playlist));
            questPlaylistProperty = serializedObject.FindProperty(nameof(PlaylistData.questPlaylist));
            trackNamesProperty = serializedObject.FindProperty(nameof(PlaylistData.trackNames));

            trackList = new ReorderableList(serializedObject, playlistProperty);
            trackList.drawHeaderCallback = DrawTrackListHeader;
            trackList.drawElementCallback = DrawTrackListItem;
            trackList.elementHeightCallback = TrackListItemHeight;
            trackList.onReorderCallbackWithDetails = TrackListReorder;
            trackList.onAddCallback = TrackAdd;
            trackList.onRemoveCallback = TrackRemove;
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(playlistNameProperty);
            EditorGUILayout.Space();

            GUIContent fallbackDesc = new GUIContent(questFallbackTypeProperty.displayName, "How to generate alternative URLs for Quest users");
            questFallbackTypeProperty.intValue = EditorGUILayout.Popup(fallbackDesc, questFallbackTypeProperty.intValue, new string[] { "None", "Jinnai System", "Custom Prefix", "Individual URLs" });

            if (questFallbackTypeProperty.intValue == PlaylistData.FALLBACK_CUSTOM)
                EditorGUILayout.PropertyField(questCustomPrefixProperty);

            if (questPlaylistProperty.arraySize != playlistProperty.arraySize)
                questPlaylistProperty.arraySize = playlistProperty.arraySize;
            if (trackNamesProperty.arraySize != playlistProperty.arraySize)
                trackNamesProperty.arraySize = playlistProperty.arraySize;

            EditorGUILayout.Space();

            trackList.DoLayoutList();

            DrawJsonImportField("Import from JSON");

            // OverrideFoldout();

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
                copyUrls();
            }
        }

        void DrawTrackListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Track List");
        }

        void DrawTrackListItem(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty url = playlistProperty.GetArrayElementAtIndex(index);
            SerializedProperty questUrl = questPlaylistProperty.GetArrayElementAtIndex(index);
            SerializedProperty name = trackNamesProperty.GetArrayElementAtIndex(index);

            EditorGUI.LabelField(new Rect(rect.x, rect.y, 30, EditorGUIUtility.singleLineHeight), $"{index + 1}.");

            EditorGUI.LabelField(new Rect(rect.x + 30, rect.y, 100, EditorGUIUtility.singleLineHeight), "Track Name");
            EditorGUI.PropertyField(new Rect(rect.x + 130, rect.y, rect.width - 130, EditorGUIUtility.singleLineHeight), name, GUIContent.none);

            EditorGUI.LabelField(new Rect(rect.x + 30, rect.y + EditorGUIUtility.singleLineHeight, 100, EditorGUIUtility.singleLineHeight), "URL");
            EditorGUI.PropertyField(new Rect(rect.x + 130, rect.y + EditorGUIUtility.singleLineHeight, rect.width - 130, EditorGUIUtility.singleLineHeight), url, GUIContent.none);

            if (questFallbackTypeProperty.intValue == PlaylistData.FALLBACK_INDIVIDUAL)
            {
                EditorGUI.LabelField(new Rect(rect.x + 30, rect.y + EditorGUIUtility.singleLineHeight * 2, 100, EditorGUIUtility.singleLineHeight), "Quest URL");
                EditorGUI.PropertyField(new Rect(rect.x + 130, rect.y + EditorGUIUtility.singleLineHeight * 2, rect.width - 130, EditorGUIUtility.singleLineHeight), questUrl, GUIContent.none);
            }
        }

        float TrackListItemHeight(int index)
        {
            int lines = 2;
            if (questFallbackTypeProperty.intValue == PlaylistData.FALLBACK_INDIVIDUAL)
                lines = 3;

            return EditorGUIUtility.singleLineHeight * lines + EditorGUIUtility.singleLineHeight * .25f;
        }

        void TrackListReorder(ReorderableList list, int oldIndex, int newIndex)
        {
            questPlaylistProperty.MoveArrayElement(oldIndex, newIndex);
            trackNamesProperty.MoveArrayElement(oldIndex, newIndex);
        }

        void TrackAdd(ReorderableList list)
        {
            int index = list.index;
            if (list.count == 0 || list.index < 0 || list.index >= list.count)
                index = -1;

            int end = list.count;
            playlistProperty.InsertArrayElementAtIndex(end);
            questPlaylistProperty.InsertArrayElementAtIndex(end);
            trackNamesProperty.InsertArrayElementAtIndex(end);

            if (index >= 0)
            {
                playlistProperty.MoveArrayElement(end, index);
                questPlaylistProperty.MoveArrayElement(end, index);
                trackNamesProperty.MoveArrayElement(end, index);
            }
        }

        void TrackRemove(ReorderableList list)
        {
            if (list.index < 0 || list.index >= list.count)
                return;

            int index = list.index;
            playlistProperty.DeleteArrayElementAtIndex(index);
            questPlaylistProperty.DeleteArrayElementAtIndex(index);
            trackNamesProperty.DeleteArrayElementAtIndex(index);
        }

        void TracksFromJson(PlaylistDataJson data)
        {
            playlistNameProperty.stringValue = data.title ?? "";

            playlistProperty.arraySize = data.entries.Length;
            questPlaylistProperty.arraySize = data.entries.Length;
            trackNamesProperty.arraySize = data.entries.Length;

            for (int i = 0; i < data.entries.Length; i++)
            {
                PlaylistDataEntryJson entry = data.entries[i];
                if (entry.url != null && entry.url.Length > 0)
                    playlistProperty.GetArrayElementAtIndex(i).boxedValue = new VRCUrl(entry.url);
                else
                    playlistProperty.GetArrayElementAtIndex(i).boxedValue = VRCUrl.Empty;

                if (entry.questUrl != null && entry.questUrl.Length > 0)
                    questPlaylistProperty.GetArrayElementAtIndex(i).boxedValue = new VRCUrl(entry.questUrl);
                else if (entry.urlForQuest != null && entry.urlForQuest.Length > 0)
                    questPlaylistProperty.GetArrayElementAtIndex(i).boxedValue = new VRCUrl(entry.urlForQuest);
                else
                    questPlaylistProperty.GetArrayElementAtIndex(i).boxedValue = VRCUrl.Empty;

                trackNamesProperty.GetArrayElementAtIndex(i).stringValue = entry.title;
            }
        }

        private void DrawJsonImportField(string label)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            jsonScroll = EditorGUILayout.BeginScrollView(jsonScroll, GUILayout.Height(130));
            jsonInput = EditorGUILayout.TextArea(jsonInput, EditorStyles.textArea, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Import", GUILayout.Width(100)))
                {
                    var parsed = parseJson(jsonInput);
                    if (parsed != null)
                    {
                        TracksFromJson(parsed);
                        jsonInput = "";
                    }
                    else
                    {
                        EditorUtility.DisplayDialog(
                            "Invalid JSON",
                            "Failed to parse Playlist JSON. Check console for details.",
                            "OK");
                    }
                }
            }
        }

        private void OverrideFoldout()
        {
            _showPlaylistFoldout = EditorGUILayout.Foldout(_showPlaylistFoldout, "Playlist Entries");
            if (_showPlaylistFoldout)
            {
                EditorGUI.indentLevel++;
                int oldCount = playlistProperty.arraySize;
                int newCount = Mathf.Max(0, EditorGUILayout.DelayedIntField("Size", playlistProperty.arraySize));
                if (newCount != oldCount)
                {
                    for (int i = oldCount; i < newCount; i++)
                    {
                        playlistProperty.InsertArrayElementAtIndex(i);
                        trackNamesProperty.InsertArrayElementAtIndex(i);
                        questPlaylistProperty.InsertArrayElementAtIndex(i);
                    }

                    serializedObject.ApplyModifiedProperties();
                }

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
                        SerializedProperty questUrl = questPlaylistProperty.GetArrayElementAtIndex(i);
                        SerializedProperty name = trackNamesProperty.GetArrayElementAtIndex(i);

                        EditorGUILayout.PropertyField(name, new GUIContent("Track Name"));
                        EditorGUILayout.PropertyField(url, new GUIContent("URL"));
                        
                        if (questFallbackTypeProperty.intValue == PlaylistData.FALLBACK_INDIVIDUAL)
                            EditorGUILayout.PropertyField(questUrl, new GUIContent("Quest URL"));

                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        private void copyUrls()
        {
            PlaylistData data = serializedObject.targetObject as PlaylistData;
            if (data.questFallbackType == PlaylistData.FALLBACK_INDIVIDUAL)
                return;

            for (int i = 0; i < data.playlist.Length; i++)
            {
                string url = data.playlist[i] != null ? data.playlist[i].Get() : null;
                string questUrl = "";

                if (data.questFallbackType == PlaylistData.FALLBACK_NONE || url == null)
                    questUrl = "";
                if (data.questFallbackType == PlaylistData.FALLBACK_JINNAI)
                    questUrl = "https://t-ne.x0.to/?url=" + url;
                else if (data.questFallbackType == PlaylistData.FALLBACK_CUSTOM)
                    questUrl = data.questCustomPrefix + url;

                data.questPlaylist[i] = new VRCUrl(questUrl);
            }

            UdonSharpEditorUtility.CopyProxyToUdon(data);
        }

        private PlaylistDataJson parseJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) {
                Debug.LogError("JSON input was empty");
                return null;
            }

            try
            {
                return JsonUtility.FromJson<PlaylistDataJson>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to parse PlaylistDataJson:\n{e}");
                return null;
            }
        }
    }
}
