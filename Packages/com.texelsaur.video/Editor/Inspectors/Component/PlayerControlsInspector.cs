using UnityEngine;

using UnityEditor;
using UdonSharpEditor;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using VRC.SDK3.Components;
using UnityEngine.Events;
using VRC.Udon;
using UnityEditor.Events;
using UnityEditor.SceneManagement;

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
        //[ObjectPath("../Audio Manager")]
        //SerializedProperty volumeControllerProperty;
        [ObjectPath("../ColorProfile")]
        SerializedProperty colorProfileProperty;

        SerializedProperty defaultUrlModeProperty;
        SerializedProperty rememberUrlModeProperty;

        [ObjectPath("MainPanel/LowerRow/InputProgress/InputField")]
        SerializedProperty urlInputProperty;

        [ObjectPath("MainPanel/UpperRow/VolumeGroup/Slider")]
        SerializedProperty volumeSliderControlProperty;
        SerializedProperty urlInputControlProperty;
        SerializedProperty progressSliderControlProperty;
        SerializedProperty syncSliderControlProperty;
        SerializedProperty queueInputControlProperty;

        [ObjectPath("MainPanel/UpperRow/ControlGroup/StopButton/IconStop"), ComponentType(typeof(Image))]
        SerializedProperty stopIconProperty;
        [ObjectPath("MainPanel/UpperRow/ControlGroup/PauseButton/IconPause"), ComponentType(typeof(Image))]
        SerializedProperty pauseIconProperty;
        SerializedProperty lockedIconProperty;
        SerializedProperty unlockedIconProperty;
        SerializedProperty loadIconProperty;
        SerializedProperty resyncIconProperty;
        SerializedProperty repeatIconProperty;
        SerializedProperty repeatOneIconProperty;
        SerializedProperty shuffleIconProperty;
        SerializedProperty infoIconProperty;
        SerializedProperty nextIconProperty;
        SerializedProperty prevIconProperty;
        SerializedProperty playlistIconProperty;
        SerializedProperty masterIconProperty;
        SerializedProperty whitelistIconProperty;
        SerializedProperty queueIconProperty;

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
        SerializedProperty titleTextProperty;
        SerializedProperty offsetTextProperty;

        SerializedProperty playlistTextProperty;

        SerializedProperty optionsPanelProperty;
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
            "InfoPanel/Fields/Mode/ButtonModeAuto",
            "InfoPanel/Fields/Mode/ButtonModeStream",
            "InfoPanel/Fields/Mode/ButtonModeVideo",
            "InfoPanel/Fields/ScreenFit/ButtonScreenFit",
            "InfoPanel/Fields/ScreenFit/ButtonScreenFitHeight",
            "InfoPanel/Fields/ScreenFit/ButtonScreenFitWidth",
            "InfoPanel/Fields/ScreenFit/ButtonScreenStretch",
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
            "MainPanel/UpperRow/ButtonGroup/RepeatButton/IconRepeatOne",
            "MainPanel/UpperRow/ButtonGroup/PlaylistButton/IconPlaylist",
            "MainPanel/UpperRow/ButtonGroup/InfoButton/IconInfo",
            "MainPanel/LowerRow/InputProgress/LoadButton/IconLoad",
            "MainPanel/LowerRow/InputProgress/MasterLockButton/IconLocked",
            "MainPanel/LowerRow/InputProgress/MasterLockButton/IconUnlocked",
            "MainPanel/LowerRow/InputProgress/InputArea/QueueButton/IconQueue",
            "InfoPanel/Fields/CurrentVideo/InputField/PlayButton/IconPlay",
            "InfoPanel/Fields/LastVideo/InputField/PlayButton/IconPlay",
        };
        string[] generalTextPaths = new string[]
        {
            "InfoPanel/VersionInfo/Text",
        };
        string[] mainTextPaths = new string[]
        {
            "MainPanel/LowerRow/InputProgress/StatusText",
            "MainPanel/LowerRow/InputProgress/InputField/TextMask/Text",
            "MainPanel/LowerRow/InputProgress/InputField/TextMask/Placeholder",
        };
        string[] subTextPaths = new string[]
        {
            "MainPanel/LowerRow/InputProgress/QueuedText",
            "MainPanel/LowerRow/InputProgress/PlaylistText",
            "MainPanel/LowerRow/InputProgress/SourceMode",
            "MainPanel/LowerRow/InputProgress/TitleText",
            "MainPanel/LowerRow/InputProgress/OffsetText",
        };
        string[] subTextIconPaths = new string[]
        {
            "MainPanel/LowerRow/InputProgress/PlayerAccess/IconMaster",
            "MainPanel/LowerRow/InputProgress/PlayerAccess/IconWhitelist",
        };

        private void OnEnable()
        {
            videoPlayerProperty = serializedObject.FindProperty(nameof(PlayerControls.videoPlayer));
            //volumeControllerProperty = serializedObject.FindProperty(nameof(PlayerControls.audioManager));
            colorProfileProperty = serializedObject.FindProperty(nameof(PlayerControls.colorProfile));

            defaultUrlModeProperty = serializedObject.FindProperty(nameof(PlayerControls.defaultUrlMode));
            rememberUrlModeProperty = serializedObject.FindProperty(nameof(PlayerControls.rememberUrlMode));

            urlInputProperty = serializedObject.FindProperty(nameof(PlayerControls.urlInput));

            volumeSliderControlProperty = serializedObject.FindProperty(nameof(PlayerControls.volumeSliderControl));
            progressSliderControlProperty = serializedObject.FindProperty(nameof(PlayerControls.progressSliderControl));
            syncSliderControlProperty = serializedObject.FindProperty(nameof(PlayerControls.syncSliderControl));
            urlInputControlProperty = serializedObject.FindProperty(nameof(PlayerControls.urlInputControl));
            queueInputControlProperty = serializedObject.FindProperty(nameof(PlayerControls.queueInputControl));

            stopIconProperty = serializedObject.FindProperty(nameof(PlayerControls.stopIcon));
            pauseIconProperty = serializedObject.FindProperty(nameof(PlayerControls.pauseIcon));
            lockedIconProperty = serializedObject.FindProperty(nameof(PlayerControls.lockedIcon));
            unlockedIconProperty = serializedObject.FindProperty(nameof(PlayerControls.unlockedIcon));
            loadIconProperty = serializedObject.FindProperty(nameof(PlayerControls.loadIcon));
            resyncIconProperty = serializedObject.FindProperty(nameof(PlayerControls.resyncIcon));
            repeatIconProperty = serializedObject.FindProperty(nameof(PlayerControls.repeatIcon));
            repeatOneIconProperty = serializedObject.FindProperty(nameof(PlayerControls.repeatOneIcon));
            shuffleIconProperty = serializedObject.FindProperty(nameof(PlayerControls.shuffleIcon));
            infoIconProperty = serializedObject.FindProperty(nameof(PlayerControls.infoIcon));
            nextIconProperty = serializedObject.FindProperty(nameof(PlayerControls.nextIcon));
            prevIconProperty = serializedObject.FindProperty(nameof(PlayerControls.prevIcon));
            playlistIconProperty = serializedObject.FindProperty(nameof(PlayerControls.playlistIcon));
            masterIconProperty = serializedObject.FindProperty(nameof(PlayerControls.masterIcon));
            whitelistIconProperty = serializedObject.FindProperty(nameof(PlayerControls.whitelistIcon));
            queueIconProperty = serializedObject.FindProperty(nameof(PlayerControls.queueIcon));

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
            titleTextProperty = serializedObject.FindProperty(nameof(PlayerControls.titleText));
            offsetTextProperty = serializedObject.FindProperty(nameof(PlayerControls.offsetText));

            playlistTextProperty = serializedObject.FindProperty(nameof(PlayerControls.playlistText));

            optionsPanelProperty = serializedObject.FindProperty(nameof(PlayerControls.optionsPanel));

            playlistPanelProperty = serializedObject.FindProperty(nameof(PlayerControls.playlistPanel));

            CheckRepairUrlInput();
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(videoPlayerProperty);
            //EditorGUILayout.PropertyField(volumeControllerProperty);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(colorProfileProperty);
            if (GUILayout.Button("Apply Color Profile"))
                UpdateColors();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(defaultUrlModeProperty);
            EditorGUILayout.PropertyField(rememberUrlModeProperty);

            CheckUrlInputValid();

            _showObjectFoldout = EditorGUILayout.Foldout(_showObjectFoldout, "Internal Object References");
            if (_showObjectFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(urlInputProperty);
                EditorGUILayout.PropertyField(volumeSliderControlProperty);
                EditorGUILayout.PropertyField(urlInputControlProperty);
                EditorGUILayout.PropertyField(progressSliderControlProperty);
                EditorGUILayout.PropertyField(syncSliderControlProperty);
                EditorGUILayout.PropertyField(queueInputControlProperty);
                EditorGUILayout.PropertyField(stopIconProperty);
                EditorGUILayout.PropertyField(pauseIconProperty);
                EditorGUILayout.PropertyField(lockedIconProperty);
                EditorGUILayout.PropertyField(unlockedIconProperty);
                EditorGUILayout.PropertyField(loadIconProperty);
                EditorGUILayout.PropertyField(resyncIconProperty);
                EditorGUILayout.PropertyField(repeatIconProperty);
                EditorGUILayout.PropertyField(repeatOneIconProperty);
                EditorGUILayout.PropertyField(shuffleIconProperty);
                EditorGUILayout.PropertyField(infoIconProperty);
                EditorGUILayout.PropertyField(nextIconProperty);
                EditorGUILayout.PropertyField(prevIconProperty);
                EditorGUILayout.PropertyField(queueIconProperty);
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
                EditorGUILayout.PropertyField(titleTextProperty);
                EditorGUILayout.PropertyField(offsetTextProperty);
                EditorGUILayout.PropertyField(playlistTextProperty);
                EditorGUILayout.PropertyField(optionsPanelProperty);
                EditorGUILayout.PropertyField(playlistPanelProperty);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();

            if (serializedObject.hasModifiedProperties)
            {
                CheckRepairUrlInput();
                serializedObject.ApplyModifiedProperties();
            }
        }

        void CheckUrlInputValid()
        {
            if (urlInputProperty.objectReferenceInstanceIDValue == 0)
            {
                EditorGUILayout.HelpBox("URL Input field is not set", MessageType.Error);
                return;
            }

            if (urlInputProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("URL Input field reference is invalid.  Your VRC SDK may be broken.\nTry reimporting the object 'VRChat SDK - Worlds/Runtime/VRCSDK/Plugins/VRCSDK3', then check this object again to attempt a self-repair.", MessageType.Error);
                return;
            }
        }

        void CheckRepairUrlInput()
        {
            PlayerControls self = (PlayerControls)serializedObject.targetObject;
            UdonBehaviour behaviour = UdonSharpEditorUtility.GetBackingUdonBehaviour(self);

            if (urlInputProperty.objectReferenceInstanceIDValue == 0)
            {
                GameObject obj = (GameObject)urlInputControlProperty.objectReferenceValue;
                if (obj != null)
                {
                    VRCUrlInputField component = obj.GetComponent<VRCUrlInputField>();
                    if (component == null)
                        return;

                    urlInputProperty.objectReferenceInstanceIDValue = component.GetInstanceID();
                }
            }

            VRCUrlInputField vrcInput = (VRCUrlInputField)urlInputProperty.objectReferenceValue;

            CheckRepairUrlInput(self, vrcInput, true);
        }

        internal static void CheckRepairUrlInput(PlayerControls playerControls, VRCUrlInputField vrcInput, bool markDirty = false)
        {
            UdonBehaviour behaviour = UdonSharpEditorUtility.GetBackingUdonBehaviour(playerControls);
            if (vrcInput == null)
                return;

            bool needsUpdate = vrcInput.textComponent == null ||
                vrcInput.placeholder == null ||
                vrcInput.onValueChanged.GetPersistentEventCount() == 0 ||
                vrcInput.onEndEdit.GetPersistentEventCount() == 0 ||
                vrcInput.navigation.mode != Navigation.Mode.None;

            if (!needsUpdate)
                return;

            Debug.LogError("VRCUrlInputField needs to be repaired");

            GameObject recordRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(vrcInput);
            if (!recordRoot)
                recordRoot = vrcInput.gameObject;

            Undo.RecordObject(recordRoot, "Repair Video Player URL Input");

            Transform mask = vrcInput.transform.Find("TextMask");
            if (vrcInput.textComponent == null && mask != null)
            {
                Transform obj = mask.Find("Text");
                if (obj != null)
                    vrcInput.textComponent = obj.GetComponent<Text>();
            }

            if (vrcInput.placeholder == null && mask != null)
            {
                Transform obj = mask.Find("Placeholder");
                if (obj != null)
                    vrcInput.placeholder = obj.GetComponent<Text>();
            }

            // LayoutElement seems to always be added at runtime (except when it's not .. rip)

            /*
            LayoutElement element = mask.GetComponentInChildren<LayoutElement>();
            if (!element)
            {
                Transform caret = mask.Find("InputField Input Caret");
                if (!caret)
                {
                    GameObject caretObj = new GameObject("InputField Input Caret");
                    caretObj.transform.parent = mask;

                    element = caretObj.AddComponent<LayoutElement>();
                    element.ignoreLayout = true;
                    element.layoutPriority = 1;

                    PrefabUtility.RecordPrefabInstancePropertyModifications(caretObj);
                }
            }
            */

            Navigation nav = vrcInput.navigation;
            nav.mode = Navigation.Mode.None;
            vrcInput.navigation = nav;

            if (vrcInput.onValueChanged.GetPersistentEventCount() == 0)
                UnityEventTools.AddStringPersistentListener(vrcInput.onValueChanged, behaviour.SendCustomEvent, "_HandleUrlInputChange");

            if (vrcInput.onEndEdit.GetPersistentEventCount() == 0)
                UnityEventTools.AddStringPersistentListener(vrcInput.onEndEdit, behaviour.SendCustomEvent, "_HandleUrlInput");

            PrefabUtility.RecordPrefabInstancePropertyModifications(vrcInput);
            PrefabUtility.RecordPrefabInstancePropertyModifications(recordRoot);

            if (markDirty)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
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
            ControlUtils.CollectObjects<Image>(pendingUpdate, root, backgroundImagePaths);
            ControlUtils.CollectObjects<Image>(pendingUpdate, root, backgroundTitleImagePaths);
            ControlUtils.CollectObjects<Image>(pendingUpdate, root, buttonBgImagePaths);
            ControlUtils.CollectObjects<Image>(pendingUpdate, root, sliderBgImagePaths);
            ControlUtils.CollectObjects<Image>(pendingUpdate, root, volumeFillBgPaths);
            ControlUtils.CollectObjects<Image>(pendingUpdate, root, volumeHandleBgPaths);
            ControlUtils.CollectObjects<Image>(pendingUpdate, root, trackerFillBgPaths);
            ControlUtils.CollectObjects<Image>(pendingUpdate, root, trackerHandleBgPaths);
            ControlUtils.CollectObjects<Image>(pendingUpdate, root, buttonIconImagePaths);
            ControlUtils.CollectObjects<Image>(pendingUpdate, root, subTextIconPaths);
            ControlUtils.CollectObjects<Text>(pendingUpdate, root, generalTextPaths);
            ControlUtils.CollectObjects<Text>(pendingUpdate, root, mainTextPaths);
            ControlUtils.CollectObjects<Text>(pendingUpdate, root, subTextPaths);

            Undo.RecordObjects(pendingUpdate.ToArray(), "Update colors");

            ControlColorProfile colorProfile = pc.colorProfile;

            ControlUtils.UpdateImages(root, backgroundImagePaths, colorProfile.backgroundColor);
            ControlUtils.UpdateImages(root, backgroundTitleImagePaths, colorProfile.backgroundTitleColor);
            ControlUtils.UpdateImages(root, buttonBgImagePaths, colorProfile.buttonBackgroundColor);
            ControlUtils.UpdateImages(root, sliderBgImagePaths, colorProfile.sliderBackgroundColor);
            ControlUtils.UpdateImages(root, volumeFillBgPaths, colorProfile.volumeFillColor);
            ControlUtils.UpdateImages(root, volumeHandleBgPaths, colorProfile.volumeHandleColor);
            ControlUtils.UpdateImages(root, trackerFillBgPaths, colorProfile.trackerFillColor);
            ControlUtils.UpdateImages(root, trackerHandleBgPaths, colorProfile.trackerHandleColor);
            ControlUtils.UpdateImages(root, buttonIconImagePaths, colorProfile.normalColor);
            ControlUtils.UpdateImages(root, subTextIconPaths, colorProfile.subTextColor);
            ControlUtils.UpdateTexts(root, generalTextPaths, colorProfile.generalTextColor);
            ControlUtils.UpdateTexts(root, mainTextPaths, colorProfile.mainTextColor);
            ControlUtils.UpdateTexts(root, subTextPaths, colorProfile.subTextColor);

            foreach (Object obj in pendingUpdate)
                PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
        }
    }
}
