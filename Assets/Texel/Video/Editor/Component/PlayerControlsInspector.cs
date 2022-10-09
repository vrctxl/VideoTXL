using UnityEngine;

using UnityEditor;
using UdonSharpEditor;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Texel
{
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class ObjectPathAttribute : System.Attribute
    {
        public string path;

        public ObjectPathAttribute(string path)
        {
            this.path = path;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class ComponentTypeAttribute : System.Attribute
    {
        public System.Type type;

        public ComponentTypeAttribute(System.Type type)
        {
            this.type = type;
        }
    }

    [CustomEditor(typeof(PlayerControls))]
    internal class PlayerControlsInspector : Editor
    {
        static bool _showObjectFoldout;

        [ObjectPath("..")]
        SerializedProperty videoPlayerProperty;
        [ObjectPath("../Audio Manager")]
        SerializedProperty volumeControllerProperty;
        [ObjectPath("../ColorProfile")]
        SerializedProperty colorProfileProperty;

        [ObjectPath("MainPanel/LowerRow/InputProgress/InputField")]
        SerializedProperty urlInputProperty;

        [ObjectPath("MainPanel/UpperRow/VolumeGroup/Slider")]
        SerializedProperty volumeSliderControlProperty;
        SerializedProperty audio2DControlProperty;
        SerializedProperty urlInputControlProperty;
        SerializedProperty progressSliderControlProperty;
        SerializedProperty syncSliderControlProperty;

        [ObjectPath("MainPanel/UpperRow/ControlGroup/StopButton/IconStop"), ComponentType(typeof(Image))]
        SerializedProperty stopIconProperty;
        [ObjectPath("MainPanel/UpperRow/ControlGroup/PauseButton/IconPause"), ComponentType(typeof(Image))]
        SerializedProperty pauseIconProperty;
        SerializedProperty lockedIconProperty;
        SerializedProperty unlockedIconProperty;
        SerializedProperty loadIconProperty;
        SerializedProperty resyncIconProperty;
        SerializedProperty repeatIconProperty;
        SerializedProperty shuffleIconProperty;
        SerializedProperty infoIconProperty;
        SerializedProperty playCurrentIconProperty;
        SerializedProperty playlastIconProperty;
        SerializedProperty nextIconProperty;
        SerializedProperty prevIconProperty;
        SerializedProperty playlistIconProperty;
        SerializedProperty masterIconProperty;
        SerializedProperty whitelistIconProperty;

        SerializedProperty muteToggleOnProperty;
        SerializedProperty muteToggleOffProperty;
        SerializedProperty audio2DToggleOnProperty;
        SerializedProperty audio2DToggleOffProperty;
        SerializedProperty volumeSliderProperty;

        SerializedProperty progressSliderProperty;
        SerializedProperty syncSliderProperty;
        SerializedProperty statusTextProperty;
        SerializedProperty urlTextProperty;
        SerializedProperty placeholderTextProperty;
        SerializedProperty modeTextProperty;
        SerializedProperty queuedTextProperty;

        SerializedProperty playlistTextProperty;

        SerializedProperty infoPanelProperty;
        SerializedProperty instanceOwnerTextProperty;
        SerializedProperty masterTextProperty;
        SerializedProperty playerOwnerTextProperty;
        SerializedProperty videoOwnerTextProperty;
        SerializedProperty currentVideoInputProperty;
        SerializedProperty lastVideoInputProperty;
        SerializedProperty currentVideoTextProperty;
        SerializedProperty lastVideoTextProperty;

        SerializedProperty playlistPanelProperty;

        string[] backgroundImagePaths = new string[] {
            "MainPanel/Background",
            "InfoPanel/Background"
        };
        string[] backgroundTitleImagePaths = new string[]
        {
            "InfoPanel/VersionInfo",
        };
        string[] sliderBgImagePaths = new string[] {
            "MainPanel/UpperRow/VolumeGroup/Background",
            "MainPanel/LowerRow/InputProgress/Background",
            "InfoPanel/Fields/CurrentVideo/InputField",
            "InfoPanel/Fields/LastVideo/InputField",
        };
        string[] volumeFillBgPaths = new string[] {
            "MainPanel/UpperRow/VolumeGroup/Slider/Fill Area/Fill",
        };
        string[] volumeHandleBgPaths = new string[] {
            "MainPanel/UpperRow/VolumeGroup/Slider/Handle Slide Area/Handle",
        };
        string[] trackerFillBgPaths = new string[] {
            "MainPanel/LowerRow/InputProgress/TrackingSlider/Fill Area/Fill",
            "MainPanel/LowerRow/InputProgress/SyncSlider/Fill Area/Fill",
        };
        string[] trackerHandleBgPaths = new string[] {
            "MainPanel/LowerRow/InputProgress/TrackingSlider/Handle Slide Area/Handle",
        };
        string[] buttonBgImagePaths = new string[] {
            "MainPanel/UpperRow/VolumeGroup/MuteButton",
            "MainPanel/UpperRow/SyncGroup/ResyncButton",
            "MainPanel/UpperRow/ControlGroup/PrevButton",
            "MainPanel/UpperRow/ControlGroup/PauseButton",
            "MainPanel/UpperRow/ControlGroup/StopButton",
            "MainPanel/UpperRow/ControlGroup/NextButton",
            "MainPanel/UpperRow/ButtonGroup/RepeatButton",
            "MainPanel/UpperRow/ButtonGroup/PlaylistButton",
            "MainPanel/UpperRow/ButtonGroup/InfoButton",
            "MainPanel/LowerRow/InputProgress/LoadButton",
            "MainPanel/LowerRow/InputProgress/MasterLockButton",
            "InfoPanel/Fields/CurrentVideo/InputField/PlayButton",
            "InfoPanel/Fields/LastVideo/InputField/PlayButton",
        };
        string[] buttonIconImagePaths = new string[]
        {
            "MainPanel/UpperRow/VolumeGroup/MuteButton/IconMuted",
            "MainPanel/UpperRow/VolumeGroup/MuteButton/IconVolume",
            "MainPanel/UpperRow/SyncGroup/ResyncButton/IconResync",
            "MainPanel/UpperRow/ControlGroup/PrevButton/IconPrev",
            "MainPanel/UpperRow/ControlGroup/PauseButton/IconPause",
            "MainPanel/UpperRow/ControlGroup/StopButton/IconStop",
            "MainPanel/UpperRow/ControlGroup/NextButton/IconNext",
            "MainPanel/UpperRow/ButtonGroup/RepeatButton/IconRepeat",
            "MainPanel/UpperRow/ButtonGroup/PlaylistButton/IconPlaylist",
            "MainPanel/UpperRow/ButtonGroup/InfoButton/IconInfo",
            "MainPanel/LowerRow/InputProgress/LoadButton/IconLoad",
            "MainPanel/LowerRow/InputProgress/MasterLockButton/IconLocked",
            "MainPanel/LowerRow/InputProgress/MasterLockButton/IconUnlocked",
            "InfoPanel/Fields/CurrentVideo/InputField/PlayButton/IconPlay",
            "InfoPanel/Fields/LastVideo/InputField/PlayButton/IconPlay",
        };
        string[] generalTextPaths = new string[]
        {
            "InfoPanel/VersionInfo/Text",
            "InfoPanel/Fields/InstanceOwner",
            "InfoPanel/Fields/InstanceOwner/InstanceOwnerName",
            "InfoPanel/Fields/Master",
            "InfoPanel/Fields/Master/MasterName",
            "InfoPanel/Fields/PlayerOwner",
            "InfoPanel/Fields/PlayerOwner/PlayerOwnerName",
            "InfoPanel/Fields/VideoOwner",
            "InfoPanel/Fields/VideoOwner/VideoOwnerName",
            "InfoPanel/Fields/CurrentVideo",
            "InfoPanel/Fields/LastVideo",
        };
        string[] mainTextPaths = new string[]
        {
            "MainPanel/LowerRow/InputProgress/StatusText",
            "MainPanel/LowerRow/InputProgress/InputField/TextMask/Text",
            "MainPanel/LowerRow/InputProgress/InputField/TextMask/Placeholder",
            "InfoPanel/Fields/CurrentVideo/InputField/TextMask/Text",
            "InfoPanel/Fields/CurrentVideo/InputField/TextMask/Placeholder",
            "InfoPanel/Fields/LastVideo/InputField/TextMask/Text",
            "InfoPanel/Fields/LastVideo/InputField/TextMask/Placeholder",
        };
        string[] subTextPaths = new string[]
        {
            "MainPanel/LowerRow/InputProgress/QueuedText",
            "MainPanel/LowerRow/InputProgress/PlaylistText",
            "MainPanel/LowerRow/InputProgress/SourceMode",
        };
        string[] subTextIconPaths = new string[]
        {
            "MainPanel/LowerRow/InputProgress/PlayerAccess/IconMaster",
            "MainPanel/LowerRow/InputProgress/PlayerAccess/IconWhitelist",
        };

        private void OnEnable()
        {
            videoPlayerProperty = serializedObject.FindProperty(nameof(PlayerControls.videoPlayer));
            volumeControllerProperty = serializedObject.FindProperty(nameof(PlayerControls.audioManager));
            colorProfileProperty = serializedObject.FindProperty(nameof(PlayerControls.colorProfile));

            urlInputProperty = serializedObject.FindProperty(nameof(PlayerControls.urlInput));

            volumeSliderControlProperty = serializedObject.FindProperty(nameof(PlayerControls.volumeSliderControl));
            audio2DControlProperty = serializedObject.FindProperty(nameof(PlayerControls.audio2DControl));
            progressSliderControlProperty = serializedObject.FindProperty(nameof(PlayerControls.progressSliderControl));
            syncSliderControlProperty = serializedObject.FindProperty(nameof(PlayerControls.syncSliderControl));
            urlInputControlProperty = serializedObject.FindProperty(nameof(PlayerControls.urlInputControl));

            stopIconProperty = serializedObject.FindProperty(nameof(PlayerControls.stopIcon));
            pauseIconProperty = serializedObject.FindProperty(nameof(PlayerControls.pauseIcon));
            lockedIconProperty = serializedObject.FindProperty(nameof(PlayerControls.lockedIcon));
            unlockedIconProperty = serializedObject.FindProperty(nameof(PlayerControls.unlockedIcon));
            loadIconProperty = serializedObject.FindProperty(nameof(PlayerControls.loadIcon));
            resyncIconProperty = serializedObject.FindProperty(nameof(PlayerControls.resyncIcon));
            repeatIconProperty = serializedObject.FindProperty(nameof(PlayerControls.repeatIcon));
            shuffleIconProperty = serializedObject.FindProperty(nameof(PlayerControls.shuffleIcon));
            infoIconProperty = serializedObject.FindProperty(nameof(PlayerControls.infoIcon));
            playCurrentIconProperty = serializedObject.FindProperty(nameof(PlayerControls.playCurrentIcon));
            playlastIconProperty = serializedObject.FindProperty(nameof(PlayerControls.playLastIcon));
            nextIconProperty = serializedObject.FindProperty(nameof(PlayerControls.nextIcon));
            prevIconProperty = serializedObject.FindProperty(nameof(PlayerControls.prevIcon));
            playlistIconProperty = serializedObject.FindProperty(nameof(PlayerControls.playlistIcon));
            masterIconProperty = serializedObject.FindProperty(nameof(PlayerControls.masterIcon));
            whitelistIconProperty = serializedObject.FindProperty(nameof(PlayerControls.whitelistIcon));

            muteToggleOnProperty = serializedObject.FindProperty(nameof(PlayerControls.muteToggleOn));
            muteToggleOffProperty = serializedObject.FindProperty(nameof(PlayerControls.muteToggleOff));
            audio2DToggleOnProperty = serializedObject.FindProperty(nameof(PlayerControls.audio2DToggleOn));
            audio2DToggleOffProperty = serializedObject.FindProperty(nameof(PlayerControls.audio2DToggleOff));
            volumeSliderProperty = serializedObject.FindProperty(nameof(PlayerControls.volumeSlider));

            statusTextProperty = serializedObject.FindProperty(nameof(PlayerControls.statusText));
            placeholderTextProperty = serializedObject.FindProperty(nameof(PlayerControls.placeholderText));
            urlTextProperty = serializedObject.FindProperty(nameof(PlayerControls.urlText));
            progressSliderProperty = serializedObject.FindProperty(nameof(PlayerControls.progressSlider));
            syncSliderProperty = serializedObject.FindProperty(nameof(PlayerControls.syncSlider));
            modeTextProperty = serializedObject.FindProperty(nameof(PlayerControls.modeText));
            queuedTextProperty = serializedObject.FindProperty(nameof(PlayerControls.queuedText));

            playlistTextProperty = serializedObject.FindProperty(nameof(PlayerControls.playlistText));

            infoPanelProperty = serializedObject.FindProperty(nameof(PlayerControls.infoPanel));
            instanceOwnerTextProperty = serializedObject.FindProperty(nameof(PlayerControls.instanceOwnerText));
            masterTextProperty = serializedObject.FindProperty(nameof(PlayerControls.masterText));
            playerOwnerTextProperty = serializedObject.FindProperty(nameof(PlayerControls.playerOwnerText));
            videoOwnerTextProperty = serializedObject.FindProperty(nameof(PlayerControls.videoOwnerText));
            currentVideoInputProperty = serializedObject.FindProperty(nameof(PlayerControls.currentVideoInput));
            lastVideoInputProperty = serializedObject.FindProperty(nameof(PlayerControls.lastVideoInput));
            currentVideoTextProperty = serializedObject.FindProperty(nameof(PlayerControls.currentVideoText));
            lastVideoTextProperty = serializedObject.FindProperty(nameof(PlayerControls.lastVideoText));

            playlistPanelProperty = serializedObject.FindProperty(nameof(PlayerControls.playlistPanel));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(videoPlayerProperty);
            EditorGUILayout.PropertyField(volumeControllerProperty);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(colorProfileProperty);
            if (GUILayout.Button("Apply Color Profile"))
                UpdateColors();

            EditorGUILayout.Space();

            _showObjectFoldout = EditorGUILayout.Foldout(_showObjectFoldout, "Internal Object References");
            if (_showObjectFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(urlInputProperty);
                EditorGUILayout.PropertyField(volumeSliderControlProperty);
                EditorGUILayout.PropertyField(audio2DControlProperty);
                EditorGUILayout.PropertyField(urlInputControlProperty);
                EditorGUILayout.PropertyField(progressSliderControlProperty);
                EditorGUILayout.PropertyField(syncSliderControlProperty);
                EditorGUILayout.PropertyField(stopIconProperty);
                EditorGUILayout.PropertyField(pauseIconProperty);
                EditorGUILayout.PropertyField(lockedIconProperty);
                EditorGUILayout.PropertyField(unlockedIconProperty);
                EditorGUILayout.PropertyField(loadIconProperty);
                EditorGUILayout.PropertyField(resyncIconProperty);
                EditorGUILayout.PropertyField(repeatIconProperty);
                EditorGUILayout.PropertyField(shuffleIconProperty);
                EditorGUILayout.PropertyField(infoIconProperty);
                EditorGUILayout.PropertyField(playCurrentIconProperty);
                EditorGUILayout.PropertyField(playlastIconProperty);
                EditorGUILayout.PropertyField(nextIconProperty);
                EditorGUILayout.PropertyField(prevIconProperty);
                EditorGUILayout.PropertyField(playlistIconProperty);
                EditorGUILayout.PropertyField(masterIconProperty);
                EditorGUILayout.PropertyField(whitelistIconProperty);
                EditorGUILayout.PropertyField(muteToggleOnProperty);
                EditorGUILayout.PropertyField(muteToggleOffProperty);
                EditorGUILayout.PropertyField(audio2DToggleOnProperty);
                EditorGUILayout.PropertyField(audio2DToggleOffProperty);
                EditorGUILayout.PropertyField(volumeSliderProperty);
                EditorGUILayout.PropertyField(progressSliderProperty);
                EditorGUILayout.PropertyField(syncSliderProperty);
                EditorGUILayout.PropertyField(statusTextProperty);
                EditorGUILayout.PropertyField(urlTextProperty);
                EditorGUILayout.PropertyField(placeholderTextProperty);
                EditorGUILayout.PropertyField(modeTextProperty);
                EditorGUILayout.PropertyField(queuedTextProperty);
                EditorGUILayout.PropertyField(playlistTextProperty);
                EditorGUILayout.PropertyField(infoPanelProperty);
                EditorGUILayout.PropertyField(instanceOwnerTextProperty);
                EditorGUILayout.PropertyField(masterTextProperty);
                EditorGUILayout.PropertyField(playerOwnerTextProperty);
                EditorGUILayout.PropertyField(videoOwnerTextProperty);
                EditorGUILayout.PropertyField(currentVideoInputProperty);
                EditorGUILayout.PropertyField(lastVideoInputProperty);
                EditorGUILayout.PropertyField(currentVideoTextProperty);
                EditorGUILayout.PropertyField(lastVideoTextProperty);
                EditorGUILayout.PropertyField(playlistPanelProperty);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();

            if (serializedObject.hasModifiedProperties)
                serializedObject.ApplyModifiedProperties();
        }

        void UpdateColors()
        {
            PlayerControls pc = (PlayerControls)serializedObject.targetObject;
            if (pc == null)
            {
                Debug.LogWarning("Could not find gameobject");
                return;
            }

            if (pc.colorProfile == null)
            {
                Debug.LogWarning("No control color profile set");
                return;
            }

            GameObject root = pc.gameObject;

            List<Object> pendingUpdate = new List<Object>();
            CollectObjects<Image>(pendingUpdate, root, backgroundImagePaths);
            CollectObjects<Image>(pendingUpdate, root, backgroundTitleImagePaths);
            CollectObjects<Image>(pendingUpdate, root, buttonBgImagePaths);
            CollectObjects<Image>(pendingUpdate, root, sliderBgImagePaths);
            CollectObjects<Image>(pendingUpdate, root, volumeFillBgPaths);
            CollectObjects<Image>(pendingUpdate, root, volumeHandleBgPaths);
            CollectObjects<Image>(pendingUpdate, root, trackerFillBgPaths);
            CollectObjects<Image>(pendingUpdate, root, trackerHandleBgPaths);
            CollectObjects<Image>(pendingUpdate, root, buttonIconImagePaths);
            CollectObjects<Image>(pendingUpdate, root, subTextIconPaths);
            CollectObjects<Text>(pendingUpdate, root, generalTextPaths);
            CollectObjects<Text>(pendingUpdate, root, mainTextPaths);
            CollectObjects<Text>(pendingUpdate, root, subTextPaths);

            Undo.RecordObjects(pendingUpdate.ToArray(), "Update colors");

            ControlColorProfile colorProfile = pc.colorProfile;

            UpdateImages(root, backgroundImagePaths, colorProfile.backgroundColor);
            UpdateImages(root, backgroundTitleImagePaths, colorProfile.backgroundTitleColor);
            UpdateImages(root, buttonBgImagePaths, colorProfile.buttonBackgroundColor);
            UpdateImages(root, sliderBgImagePaths, colorProfile.sliderBackgroundColor);
            UpdateImages(root, volumeFillBgPaths, colorProfile.volumeFillColor);
            UpdateImages(root, volumeHandleBgPaths, colorProfile.volumeHandleColor);
            UpdateImages(root, trackerFillBgPaths, colorProfile.trackerFillColor);
            UpdateImages(root, trackerHandleBgPaths, colorProfile.trackerHandleColor);
            UpdateImages(root, buttonIconImagePaths, colorProfile.normalColor);
            UpdateImages(root, subTextIconPaths, colorProfile.subTextColor);
            UpdateTexts(root, generalTextPaths, colorProfile.generalTextColor);
            UpdateTexts(root, mainTextPaths, colorProfile.mainTextColor);
            UpdateTexts(root, subTextPaths, colorProfile.subTextColor);

            foreach (Object obj in pendingUpdate)
                PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
        }

        void CollectObjects<T>(List<Object> list, GameObject root, string[] paths) where T : Object
        {
            foreach (string path in paths)
            {
                Transform t = root.transform.Find(path);
                if (t == null)
                    continue;

                T component = t.GetComponent<T>();
                if (component == null)
                    continue;

                list.Add(component);
            }
        }

        void UpdateImages(GameObject root, string[] paths, Color color)
        {
            foreach (string path in paths)
            {
                Transform t = root.transform.Find(path);
                if (t == null)
                    continue;

                Image image = t.GetComponent<Image>();
                if (image == null)
                    continue;

                image.color = color;
            }
        }

        void UpdateTexts(GameObject root, string[] paths, Color color)
        {
            foreach (string path in paths)
            {
                Transform t = root.transform.Find(path);
                if (t == null)
                    continue;

                Text text = t.GetComponent<Text>();
                if (text == null)
                    continue;

                text.color = color;
            }
        }

        void Repair()
        {
            FieldInfo iconField = GetType().GetField("stopIconProperty", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var x in System.Attribute.GetCustomAttributes(iconField))
            {
                if (x is ObjectPathAttribute)
                {
                    //Component c = FindComponent(serializedObject.(x as ObjectPathAttribute).path)
                }
            }
        }

        GameObject FindGameObject(GameObject root, string path)
        {
            while (path.StartsWith(".."))
            {
                if (root.transform.parent == null)
                    return null;

                root = root.transform.parent.gameObject;
                path = Regex.Replace(path, "^../?", "");
            }

            Transform t = root.transform.Find(path);
            if (t == null)
                return null;

            return t.gameObject;
        }

        Component FindComponent(GameObject root, string path, System.Type type)
        {
            GameObject obj = FindGameObject(root, path);
            if (obj == null)
                return null;

            return obj.transform.GetComponent(type);
        }
    }
}
