using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.Udon;

namespace Texel
{
    [CustomEditor(typeof(InputProxy))]
    public class InputProxyEditor : Editor
    {
        SerializedProperty videoPlayerProperty;
        SerializedProperty alwaysUseQueueProperty;

        SerializedProperty urlInputFieldProperty;
        SerializedProperty urlNameInputFieldProperty;
        SerializedProperty urlAuthorInputFieldProperty;
        SerializedProperty queueInputFieldProperty;
        SerializedProperty queueNameInputFieldProperty;
        SerializedProperty queueAuthorInputFieldProperty;

        SerializedProperty youtubeSearchManangerProperty;
        SerializedProperty youtubeSearchEnabledProperty;

        bool showInternal = false;
        UdonBehaviour youtubeSearchManagerCache;

        static bool expandIntegrations = true;
        static string integrationsUrl = "https://vrctxl.github.io/Docs/docs/video-txl/configuration/input-proxy#integrations";

        void OnEnable()
        {
            videoPlayerProperty = serializedObject.FindProperty(nameof(InputProxy.videoPlayer));
            alwaysUseQueueProperty = serializedObject.FindProperty(nameof(InputProxy.alwaysUseQueue));

            urlInputFieldProperty = serializedObject.FindProperty(nameof(InputProxy.urlInputField));
            urlNameInputFieldProperty = serializedObject.FindProperty(nameof(InputProxy.urlNameInputField));
            urlAuthorInputFieldProperty = serializedObject.FindProperty(nameof(InputProxy.urlAuthorInputField));

            queueInputFieldProperty = serializedObject.FindProperty(nameof(InputProxy.queueInputField));
            queueNameInputFieldProperty = serializedObject.FindProperty(nameof(InputProxy.queueNameInputField));
            queueAuthorInputFieldProperty = serializedObject.FindProperty(nameof(InputProxy.queueAuthorInputField));

            youtubeSearchManangerProperty = serializedObject.FindProperty(nameof(InputProxy.youtubeSearchManager));
            youtubeSearchEnabledProperty = serializedObject.FindProperty(nameof(InputProxy.youtubeSearchEnabled));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(videoPlayerProperty);
            EditorGUILayout.PropertyField(alwaysUseQueueProperty);

            showInternal = EditorGUILayout.Foldout(showInternal, "Internal", true, EditorStyles.foldoutHeader);
            if (showInternal)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField("URL Input Fields", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(urlInputFieldProperty);
                EditorGUILayout.PropertyField(urlNameInputFieldProperty);
                EditorGUILayout.PropertyField(urlAuthorInputFieldProperty);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Queue Input Fields", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(queueInputFieldProperty);
                EditorGUILayout.PropertyField(queueNameInputFieldProperty);
                EditorGUILayout.PropertyField(queueAuthorInputFieldProperty);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            IntegrationsSection();

            serializedObject.ApplyModifiedProperties();
        }

        private UdonBehaviour FindYoutubeSearchManager()
        {
            youtubeSearchManagerCache = TXLUdon.FindExternal(youtubeSearchManagerCache, "YoutubeSearchManager");
            return youtubeSearchManagerCache;
        }

        void LinkYoutubeSearch()
        {
            TXLUdon.LinkProperty(youtubeSearchManangerProperty, FindYoutubeSearchManager());
            if (youtubeSearchManagerCache)
            {
                UdonSharpBehaviour sharp = UdonSharpEditorUtility.GetProxyBehaviour(youtubeSearchManagerCache);
                if (sharp)
                {
                    Undo.RecordObjects(new Object[] { youtubeSearchManagerCache, sharp }, "Link Youtube Search");

                    UdonBehaviour uiController = (UdonBehaviour)sharp.GetProgramVariable("VideoPlayerUIController");
                    sharp.SetProgramVariable("VideoPlayerUIController", TXLUdon.FindBehaviour((InputProxy)target, "InputProxy"));
                    sharp.SetProgramVariable("UrlInputField", urlInputFieldProperty.objectReferenceValue);
                    sharp.SetProgramVariable("VideoNameInputField", urlNameInputFieldProperty.objectReferenceValue);
                    sharp.SetProgramVariable("OtherVideoplayerRequredPlayEvents", "_UrlInput");

                    sharp.SetProgramVariable("QueueUIController", TXLUdon.FindBehaviour((InputProxy)target, "InputProxy"));
                    sharp.SetProgramVariable("UrlInputFieldQueue", queueInputFieldProperty.objectReferenceValue);
                    sharp.SetProgramVariable("VideoNameInputFieldQueue", queueNameInputFieldProperty.objectReferenceValue);
                    sharp.SetProgramVariable("videoPlayerType", 6);
                    sharp.SetProgramVariable("QueueRequiredPlayEvents", "_QueueInput");

                    UdonSharpEditorUtility.CopyProxyToUdon(sharp);
                    EditorUtility.SetDirty(youtubeSearchManagerCache);
                    EditorUtility.SetDirty(sharp);
                }
            }
        }

        private void IntegrationsSection()
        {
            if (!TXLEditor.DrawMainHeaderHelp(new GUIContent("External Systems"), ref expandIntegrations, integrationsUrl))
                return;

            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField(new GUIContent("Youtube Search Prefab"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(youtubeSearchEnabledProperty, new GUIContent("Enabled", "Whether the Youtube Search integration is enabled or not"));

            if (youtubeSearchEnabledProperty.boolValue)
            {
                EditorGUILayout.PropertyField(youtubeSearchManangerProperty, new GUIContent("Youtube Search Manager", "The YoutubeSearchManager component in your scene"));

                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                if (GUILayout.Button(new GUIContent("Link Youtube Search", "Finds YoutubeSearch in the scene and automatically configures it for this video player.")))
                    LinkYoutubeSearch();
                GUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }
    }
}
