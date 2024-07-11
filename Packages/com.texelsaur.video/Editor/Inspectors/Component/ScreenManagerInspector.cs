
using UnityEngine;

using UnityEditor;
using UdonSharpEditor;
using VRC.Udon;
using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UdonSharp;

namespace Texel
{
    internal class EditorScreenPropertyMap
    {
        public string screenTexture;
        public string avProCheck;
        public string invertY;
        public string applyGamma;
        public string screenFit;
        public string aspectRatio;
        public string targetAspectRatio;
        public string doubleBuffered;

        public EditorScreenPropertyMap() { }

        public static EditorScreenPropertyMap FromPropertyMap(ScreenPropertyMap map)
        {
            if (!map)
                return null;

            EditorScreenPropertyMap emap = new EditorScreenPropertyMap();
            emap.screenTexture = map.screenTexture;
            emap.avProCheck = map.avProCheck;
            emap.invertY = map.invertY;
            emap.applyGamma = map.applyGamma;
            emap.screenFit = map.screenFit;
            emap.aspectRatio = map.aspectRatio;
            emap.targetAspectRatio = map.targetAspectRatio;
            emap.doubleBuffered = map.doubleBuffered;

            return emap;
        }

        public static EditorScreenPropertyMap FromMaterial(Material mat)
        {
            if (!mat)
                return null;

            return FromShader(mat.shader.name);
        }

        public static EditorScreenPropertyMap FromShader(string shaderName)
        {
            EditorScreenPropertyMap map = null;
            switch (shaderName)
            {
                case "VideoTXL/RealtimeEmissiveGamma":
                case "VideoTXL/Unlit":
                    map = new EditorScreenPropertyMap();
                    map.screenTexture = "_MainTex";
                    map.avProCheck = "_IsAVProInput";
                    map.invertY = "_InvertAVPro";
                    map.applyGamma = "_ApplyGammaAVPro";
                    map.screenFit = "_FitMode";
                    map.aspectRatio = "_TexAspectRatio";
                    return map;
                case "VideoTXL/RenderOut":
                    map = new EditorScreenPropertyMap();
                    map.screenTexture = "_MainTex";
                    map.invertY = "_FlipY";
                    map.applyGamma = "_ApplyGamma";
                    map.screenFit = "_FitMode";
                    map.aspectRatio = "_TexAspectRatio";
                    map.targetAspectRatio = "_AspectRatio";
                    map.doubleBuffered = "_DoubleBuffered";
                    return map;
                default:
                    return map;
            }
        }
    }

    // TODO: Checks on overrides
    // [ ] Do all overrides have a mapping profile selected?
    // [ ] Do property override objects have material with CRT set?  That is probably a mixup
    // [X] Update CRT at edittime for logo?

    [CustomEditor(typeof(ScreenManager))]
    [InitializeOnLoad]
    internal class ScreenManagerInspector : Editor
    {
        const string DEFAULT_CRT_PATH = "Packages/com.texelsaur.video/Runtime/RenderTextures/VideoTXLCRT.asset";
        const string DEFAULT_CRT_MAT_PATH = "Packages/com.texelsaur.video/Runtime/Materials/StreamOutput.mat";
        const string DEFAULT_SCREEN_MAT_PATH = "Packages/com.texelsaur.video/Runtime/Materials/StandardMaterialScreen.mat";
        const string DEFAULT_VRSL_MAT_PATH = "Packages/com.texelsaur.video/Runtime/Materials/StreamOutputVRSL.mat";

        static readonly string[] PLACEHOLDER_IMAGE_PATHS = {
            "Packages/com.texelsaur.video/Runtime/Textures/Placeholder Screens/Error.jpg",
            "Packages/com.texelsaur.video/Runtime/Textures/Placeholder Screens/ErrorBlocked.jpg",
            "Packages/com.texelsaur.video/Runtime/Textures/Placeholder Screens/ErrorInvalid.jpg",
            "Packages/com.texelsaur.video/Runtime/Textures/Placeholder Screens/ErrorRateLimited.jpg",
            "Packages/com.texelsaur.video/Runtime/Textures/Placeholder Screens/ScreenAudio.jpg",
            "Packages/com.texelsaur.video/Runtime/Textures/Placeholder Screens/ScreenBlack.jpg",
            "Packages/com.texelsaur.video/Runtime/Textures/Placeholder Screens/ScreenLoading.jpg",
            "Packages/com.texelsaur.video/Runtime/Textures/Placeholder Screens/ScreenSynchronizing.jpg",
            "Packages/com.texelsaur.video/Runtime/Textures/Placeholder Screens/ScreenTXL.png",
        };

        struct PropBlockEntry
        {
            public MeshRenderer renderer;
            public bool useMaterialOverride;
            public int materialIndex;
            public ScreenPropertyMap propertyMap;

            public PropBlockEntry(ScreenManager manager, int index)
            {
                renderer = (manager?.propMeshList?.Length ?? 0) > index ? manager.propMeshList[index] : null;
                useMaterialOverride = (manager?.propMaterialOverrideList?.Length ?? 0) > index ? manager.propMaterialOverrideList[index] == 1 : false;
                materialIndex = (manager?.propMaterialIndexList?.Length ?? 0) > index ? manager.propMaterialIndexList[index] : 0;
                propertyMap = (manager?.propPropertyList?.Length ?? 0) > index ? manager.propPropertyList[index] : null;
            }
        }

        static bool _showErrorMatFoldout;
        static bool _showErrorTexFoldout;
        static bool _showCrtAdvanced = false;

        SerializedProperty videoPlayerProperty;

        SerializedProperty debugLogProperty;
        SerializedProperty debugStateProperty;
        SerializedProperty vrcLoggingProperty;
        SerializedProperty lowLevelLoggingProperty;
        SerializedProperty eventLoggingProperty;

        SerializedProperty playbackMaterialProperty;
        SerializedProperty logoMaterialProperty;
        SerializedProperty loadingMaterialProperty;
        SerializedProperty syncMaterialProperty;
        SerializedProperty audioMaterialProperty;
        SerializedProperty errorMaterialProperty;
        SerializedProperty errorInvalidMaterialProperty;
        SerializedProperty errorBlockedMaterialProperty;
        SerializedProperty errorRateLimitedMaterialProperty;

        SerializedProperty latchErrorStateProperty;
        SerializedProperty overrideAspectRatioProperty;
        SerializedProperty aspectRatioProperty;
        SerializedProperty logoTextureProperty;
        SerializedProperty loadingTextureProperty;
        SerializedProperty syncTextureProperty;
        SerializedProperty audioTextureProperty;
        SerializedProperty errorTextureProperty;
        SerializedProperty errorInvalidTextureProperty;
        SerializedProperty errorBlockedTextureProperty;
        SerializedProperty errorRateLimitedTextureProperty;
        SerializedProperty editorTextureProperty;

        SerializedProperty renderOutCrtListProperty;
        SerializedProperty renderOutMatPropsListProperty;
        SerializedProperty renderOutSizeListProperty;
        SerializedProperty renderOutTargetAspectListProperty;
        SerializedProperty renderOutResizeListProperty;
        SerializedProperty renderOutExpandSizeListProperty;
        SerializedProperty renderOutGlobalTexListProperty;
        SerializedProperty renderOutDoubleBufferAVProListProperty;
        SerializedProperty renderOutDoubleBufferUnityListProperty;

        SerializedProperty downloadLogoImageProperty;
        SerializedProperty downloadLogoImageUrlProperty;

        // VRSL Integration
        SerializedProperty vrslEnabledProperty;
        SerializedProperty vrslControllerProperty;
        SerializedProperty vrslDmxRTProperty;
        SerializedProperty vrslOffsetScaleProperty;
        SerializedProperty vrslSourceAspectRatioProperty;
        SerializedProperty vrslBlitMatProperty;
        SerializedProperty vrslDoubleBufferAVProProperty;
        SerializedProperty vrslDoubleBufferUnityProperty;

        SerializedProperty _udonSharpBackingUdonBehaviourProperty;

        // Legacy
        SerializedProperty useRenderOutProperty;
        SerializedProperty outputCRTProperty;
        SerializedProperty outputMaterialPropertiesProperty;

        CrtListDisplay crtList;
        SharedMaterialListDisplay sharedMaterialList;
        PropBlockListDisplay propBlockList;
        ObjectOverrideListDisplay objectOverrideList;
        GlobalPropertyListDisplay globalPropList;

        bool vrslOutsideLinked = false;
        UdonBehaviour vrslControllerCache;

        static Material vrslEditorBlitMat = null;

        static bool expandTextures = true;
        static bool expandObjectOverrides = false;
        static bool expandDebug = false;
        static bool expandIntegrations = false;
        static List<ScreenManager> managers;
        static RenderTexture videoTexRT;

        static string mainHelpUrl = "https://vrctxl.github.io/Docs/docs/video-txl/configuration/screen-manager";
        static string textureOverridesUrl = "https://vrctxl.github.io/Docs/docs/video-txl/configuration/screen-manager#texture-overrides";
        static string objectMaterialsUrl = "https://vrctxl.github.io/Docs/docs/video-txl/configuration/screen-manager#object-material-overrides";
        static string debugOptionsUrl = "https://vrctxl.github.io/Docs/docs/video-txl/configuration/screen-manager#debug-options";
        static string integrationsUrl = "https://vrctxl.github.io/Docs/docs/video-txl/configuration/screen-manager#integrations";

        static ScreenManagerInspector()
        {
            EditorSceneManager.activeSceneChangedInEditMode += OnSceneLoaded;
            AssemblyReloadEvents.afterAssemblyReload += OnAssemblyReload;
        }

        static void OnSceneLoaded(Scene prevScene, Scene newScene)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene == null || scene.name == "")
                return;

            Debug.Log("SceneLoaded");
            UpdateManagers();
            UpdateEditorTextures();
        }

        static void OnAssemblyReload()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene == null || scene.name == "")
                return;

            Debug.Log("AssemblyReload");
            UpdateManagers();
            UpdateEditorTextures();
        }

        static void UpdateManagers()
        {
            ScreenManager[] found = FindObjectsOfType<ScreenManager>();

            managers = new List<ScreenManager>();
            managers.AddRange(found);

            Debug.Log($"[VideoTXL] Found {managers.Count} ScreenManagers in scene");
        }

        static void UpdateEditorTextures()
        {
            foreach (ScreenManager manager in managers)
            {
                if (manager == null)
                    continue;

                UpdateEditorMaterialBlocks(manager);
                UpdateEditorSharedMaterials(manager);
                UpdateEditorCRT(manager);
                UpdateEditorVRSL(manager);
            }

            EditorWindow view = EditorWindow.GetWindow<SceneView>();
            if (view)
                view.Repaint();
        }

        private void OnEnable()
        {
            videoPlayerProperty = serializedObject.FindProperty(nameof(ScreenManager.videoPlayer));

            debugLogProperty = serializedObject.FindProperty(nameof(ScreenManager.debugLog));
            debugStateProperty = serializedObject.FindProperty(nameof(ScreenManager.debugState));
            vrcLoggingProperty = serializedObject.FindProperty(nameof(ScreenManager.vrcLogging));
            lowLevelLoggingProperty = serializedObject.FindProperty(nameof(ScreenManager.lowLevelLogging));
            eventLoggingProperty = serializedObject.FindProperty(nameof(ScreenManager.eventLogging));

            playbackMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.playbackMaterial));
            logoMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.logoMaterial));
            loadingMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.loadingMaterial));
            syncMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.syncMaterial));
            audioMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.audioMaterial));
            errorMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.errorMaterial));
            errorInvalidMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.errorInvalidMaterial));
            errorBlockedMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.errorBlockedMaterial));
            errorRateLimitedMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.errorRateLimitedMaterial));

            latchErrorStateProperty = serializedObject.FindProperty(nameof(ScreenManager.latchErrorState));
            overrideAspectRatioProperty = serializedObject.FindProperty(nameof(ScreenManager.overrideAspectRatio));
            aspectRatioProperty = serializedObject.FindProperty(nameof(ScreenManager.aspectRatio));
            logoTextureProperty = serializedObject.FindProperty(nameof(ScreenManager.logoTexture));
            loadingTextureProperty = serializedObject.FindProperty(nameof(ScreenManager.loadingTexture));
            syncTextureProperty = serializedObject.FindProperty(nameof(ScreenManager.syncTexture));
            audioTextureProperty = serializedObject.FindProperty(nameof(ScreenManager.audioTexture));
            errorTextureProperty = serializedObject.FindProperty(nameof(ScreenManager.errorTexture));
            errorInvalidTextureProperty = serializedObject.FindProperty(nameof(ScreenManager.errorInvalidTexture));
            errorBlockedTextureProperty = serializedObject.FindProperty(nameof(ScreenManager.errorBlockedTexture));
            errorRateLimitedTextureProperty = serializedObject.FindProperty(nameof(ScreenManager.errorRateLimitedTexture));
            editorTextureProperty = serializedObject.FindProperty(nameof(ScreenManager.editorTexture));

            useRenderOutProperty = serializedObject.FindProperty(nameof(ScreenManager.useRenderOut));
            outputCRTProperty = serializedObject.FindProperty(nameof(ScreenManager.outputCRT));
            outputMaterialPropertiesProperty = serializedObject.FindProperty(nameof(ScreenManager.outputMaterialProperties));
            renderOutCrtListProperty = serializedObject.FindProperty(nameof(ScreenManager.renderOutCrt));
            renderOutMatPropsListProperty = serializedObject.FindProperty(nameof(ScreenManager.renderOutMatProps));
            renderOutSizeListProperty = serializedObject.FindProperty(nameof(ScreenManager.renderOutSize));
            renderOutTargetAspectListProperty = serializedObject.FindProperty(nameof(ScreenManager.renderOutTargetAspect));
            renderOutResizeListProperty = serializedObject.FindProperty(nameof(ScreenManager.renderOutResize));
            renderOutExpandSizeListProperty = serializedObject.FindProperty(nameof(ScreenManager.renderOutExpandSize));
            renderOutGlobalTexListProperty = serializedObject.FindProperty(nameof(ScreenManager.renderOutGlobalTex));
            renderOutDoubleBufferAVProListProperty = serializedObject.FindProperty(nameof(ScreenManager.renderOutDoubleBufferAVPro));
            renderOutDoubleBufferUnityListProperty = serializedObject.FindProperty(nameof(ScreenManager.renderOutDoubleBufferUnity));

            downloadLogoImageProperty = serializedObject.FindProperty(nameof(ScreenManager.downloadLogoImage));
            downloadLogoImageUrlProperty = serializedObject.FindProperty(nameof(ScreenManager.downloadLogoImageUrl));

            vrslEnabledProperty = serializedObject.FindProperty(nameof(ScreenManager.vrslEnabled));
            vrslControllerProperty = serializedObject.FindProperty(nameof(ScreenManager.vrslController));
            vrslDmxRTProperty = serializedObject.FindProperty(nameof(ScreenManager.vrslDmxRT));
            vrslOffsetScaleProperty = serializedObject.FindProperty(nameof(ScreenManager.vrslOffsetScale));
            vrslSourceAspectRatioProperty = serializedObject.FindProperty(nameof(ScreenManager.vrslSourceAspectRatio));
            vrslBlitMatProperty = serializedObject.FindProperty(nameof(ScreenManager.vrslBlitMat));
            vrslDoubleBufferAVProProperty = serializedObject.FindProperty(nameof(ScreenManager.vrslDoubleBufferAVPro));
            vrslDoubleBufferUnityProperty = serializedObject.FindProperty(nameof(ScreenManager.vrslDoubleBufferUnity));

            _udonSharpBackingUdonBehaviourProperty = serializedObject.FindProperty("_udonSharpBackingUdonBehaviour");

            crtList = new CrtListDisplay(serializedObject, (ScreenManager)target, this);
            sharedMaterialList = new SharedMaterialListDisplay(serializedObject, (ScreenManager)target);
            propBlockList = new PropBlockListDisplay(serializedObject, (ScreenManager)target);
            objectOverrideList = new ObjectOverrideListDisplay(serializedObject, (ScreenManager)target);
            globalPropList = new GlobalPropertyListDisplay(serializedObject, (ScreenManager)target);

            if (vrslBlitMatProperty.objectReferenceValue) {
                if (vrslEditorBlitMat)
                    vrslEditorBlitMat.CopyPropertiesFromMaterial((Material)vrslBlitMatProperty.objectReferenceValue);
                else
                    vrslEditorBlitMat = new Material((Material)vrslBlitMatProperty.objectReferenceValue);
            }

            if (renderOutDoubleBufferAVProListProperty.arraySize < renderOutCrtListProperty.arraySize)
            {
                renderOutDoubleBufferAVProListProperty.arraySize = renderOutCrtListProperty.arraySize;
                for (int i = 0; i < renderOutCrtListProperty.arraySize; i++)
                {
                    CustomRenderTexture crt = (CustomRenderTexture)renderOutCrtListProperty.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (crt)
                        renderOutDoubleBufferAVProListProperty.GetArrayElementAtIndex(i).boolValue = crt.doubleBuffered;
                }
                serializedObject.ApplyModifiedProperties();
            }

            if (renderOutDoubleBufferUnityListProperty.arraySize < renderOutCrtListProperty.arraySize)
            {
                renderOutDoubleBufferUnityListProperty.arraySize = renderOutCrtListProperty.arraySize;
                for (int i = 0; i < renderOutCrtListProperty.arraySize; i++)
                {
                    CustomRenderTexture crt = (CustomRenderTexture)renderOutCrtListProperty.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (crt)
                        renderOutDoubleBufferUnityListProperty.GetArrayElementAtIndex(i).boolValue = crt.doubleBuffered;
                }
                serializedObject.ApplyModifiedProperties();
            }

            // CRT texture
            UpdateEditorState();

            // Upgrade legacy entries
            UpgradeLegacyCrtEntry();

            vrslOutsideLinked = IsVRSLOnAnotherManager();
            FindExternal();
        }

        static SerializedProperty GetElementSafe(SerializedProperty arr, int index)
        {
            if (arr.arraySize <= index)
                arr.arraySize = index + 1;
            return arr.GetArrayElementAtIndex(index);
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            if (GUILayout.Button("Screen Manager Documentation"))
                Application.OpenURL(mainHelpUrl);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(videoPlayerProperty);

            // ---

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            TextureOverrideSection();

            EditorGUILayout.Space();
            crtList.Section();

            EditorGUILayout.Space();
            sharedMaterialList.Section();

            EditorGUILayout.Space();
            propBlockList.Section();

            EditorGUILayout.Space();
            globalPropList.Section();

            // ---

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            ObjectMaterialSection();

            // ---

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            IntegrationsSection();

            // ---

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            DebugSection();

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
                UpdateEditorState();
            }
        }

        private void TextureOverrideSection()
        {
            if (!TXLEditor.DrawMainHeaderHelp(new GUIContent("Texture Overrides"), ref expandTextures, textureOverridesUrl))
                return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(logoTextureProperty);
            EditorGUILayout.PropertyField(loadingTextureProperty);
            // EditorGUILayout.PropertyField(syncTextureProperty);
            EditorGUILayout.PropertyField(audioTextureProperty);
            EditorGUILayout.PropertyField(errorTextureProperty);

            _showErrorTexFoldout = EditorGUILayout.Foldout(_showErrorTexFoldout, "Error Texture Overrides");
            if (_showErrorTexFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(errorInvalidTextureProperty);
                EditorGUILayout.PropertyField(errorBlockedTextureProperty);
                EditorGUILayout.PropertyField(errorRateLimitedTextureProperty);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(editorTextureProperty);

            if (UsingNonDefaultPackageTexture())
                EditorGUILayout.HelpBox("One or more textures are stored in the VideoTXL package folder, but are not shipped with the package.  They will be lost when the VideoTXL package is updated.", MessageType.Warning);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(latchErrorStateProperty);
            EditorGUILayout.PropertyField(overrideAspectRatioProperty);
            if (overrideAspectRatioProperty.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(aspectRatioProperty);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(downloadLogoImageProperty, new GUIContent("Download Logo Texture", "When enabled, attempts to download an image from a URL to use as the logo texture, replacing the existing override.  The existing override will continue to be used until the download completes or if the download fails."));
            if (downloadLogoImageProperty.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(downloadLogoImageUrlProperty, new GUIContent("Logo Texture URL"));
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
        }

        private void ObjectMaterialSection()
        {
            if (!TXLEditor.DrawMainHeaderHelp(new GUIContent("Object Material Overrides"), ref expandObjectOverrides, objectMaterialsUrl))
                return;

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(playbackMaterialProperty);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(logoMaterialProperty);
            EditorGUILayout.PropertyField(loadingMaterialProperty);
            // EditorGUILayout.PropertyField(syncMaterialProperty);
            EditorGUILayout.PropertyField(audioMaterialProperty);
            EditorGUILayout.PropertyField(errorMaterialProperty);

            _showErrorMatFoldout = EditorGUILayout.Foldout(_showErrorMatFoldout, "Error Material Overrides");
            if (_showErrorMatFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(errorInvalidMaterialProperty);
                EditorGUILayout.PropertyField(errorBlockedMaterialProperty);
                EditorGUILayout.PropertyField(errorRateLimitedMaterialProperty);
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            objectOverrideList.Section();
        }

        private void DebugSection()
        {
            if (!TXLEditor.DrawMainHeaderHelp(new GUIContent("Debug Options"), ref expandDebug, debugOptionsUrl))
                return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(debugLogProperty, new GUIContent("Debug Log", "Log debug statements to a world object"));
            EditorGUILayout.PropertyField(debugStateProperty, new GUIContent("Debug State", "Track periodically refreshed internal state in a world object"));
            EditorGUILayout.PropertyField(eventLoggingProperty, new GUIContent("Include Events", "Include additional event traffic in debug log"));
            EditorGUILayout.PropertyField(lowLevelLoggingProperty, new GUIContent("Include Low Level", "Include additional verbose messages in debug log"));
            EditorGUILayout.PropertyField(vrcLoggingProperty, new GUIContent("VRC Logging", "Write out debug messages to VRChat log."));
            EditorGUI.indentLevel--;
        }

        static GUIContent doubleBufferLabel = new GUIContent("Double Bufferd", "Use double buffering with AVPro video sources to repeat previous frames whenever a frame is dropped.");
        static GUIContent unityLabel = new GUIContent("Unity");
        static GUIContent avproLabel = new GUIContent("AVPro");

        private void IntegrationsSection()
        {
            if (!TXLEditor.DrawMainHeaderHelp(new GUIContent("External Systems"), ref expandIntegrations, integrationsUrl))
                return;

            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField(new GUIContent("VRSL"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(vrslEnabledProperty, new GUIContent("Enabled", "Whether the VRSL integration is enabled or not"));

            if (vrslEnabledProperty.boolValue)
            {
                EditorGUILayout.PropertyField(vrslControllerProperty, new GUIContent("Local UI Control Panel", "The VRSL_LocalUIControlPanel in your scene"));
                EditorGUILayout.PropertyField(vrslDmxRTProperty, new GUIContent("DMX Raw RT", "The VRSL raw RenderTexture for either horizontal, vertical, or legacy configuration"));
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(vrslBlitMatProperty, new GUIContent("DMX Copy material", "The material used to copy the DMX section of the video feed to the DMX Raw RT.  Do not change unless you know what you're doing."));
                EditorGUILayout.PropertyField(vrslSourceAspectRatioProperty, new GUIContent("Source Aspect Ratio", "The expected aspect ratio of streams containing VRSL DMX Grid data"));
                EditorGUI.indentLevel--;

                float aspectRatio = vrslSourceAspectRatioProperty.floatValue;
                if (aspectRatio <= 0)
                {
                    aspectRatio = 1.777777f;
                    if (overrideAspectRatioProperty.boolValue && aspectRatioProperty.floatValue > 0)
                        aspectRatio = aspectRatioProperty.floatValue;

                    vrslSourceAspectRatioProperty.floatValue = aspectRatio;
                }

                RenderTexture rt = (RenderTexture)vrslDmxRTProperty.objectReferenceValue;
                if (rt != null)
                {
                    EditorGUILayout.LabelField("DMX Area");
                    EditorGUILayout.Space(3);

                    // Horz: 104 x 960
                    // Vert: 104 x 540

                    bool vertical = rt.height == 540;
                    float dmxW = 1;
                    float dmxH = (208f / 1080) * (aspectRatio / 1.77777f);
                    if (vertical)
                    {
                        dmxW = (208f / 1920) * (aspectRatio / 1.77777f);
                        dmxH = 1;
                    }

                    Vector3 offsetScale = vrslOffsetScaleProperty.vector3Value;

                    float editWidth = Math.Min(EditorGUIUtility.currentViewWidth - 45 - 50, 400f);
                    float editHeight = editWidth / aspectRatio;

                    Texture bgTex = (Texture)editorTextureProperty.objectReferenceValue;
                    if (bgTex == null)
                        bgTex = Texture2D.blackTexture;

                    float requestHeight = editHeight;
                    //if (vertical)
                    requestHeight += 30;

                    Rect rect = GUILayoutUtility.GetRect(editWidth, requestHeight);
                    rect.x += 30;
                    rect.width -= 30; // Margin
                    rect.width -= 20; // Slider

                    Rect boxRect = rect;
                    boxRect.width = Math.Min(editWidth, rect.width);
                    boxRect.height = boxRect.width / aspectRatio;
                    boxRect.x = rect.x;

                    Rect asRect = new Rect(boxRect.x + 1, boxRect.y + 1, boxRect.width - 2, boxRect.height - 2);
                    if (!vertical)
                    {
                        float diffH = asRect.height - (asRect.height * dmxH);
                        asRect.height = (asRect.height - diffH) * offsetScale.z;
                        asRect.y += diffH * (1 - offsetScale.y);
                        asRect.width *= offsetScale.z;
                        asRect.x += (boxRect.width - 2 - asRect.width) * offsetScale.x;
                    }
                    else
                    {
                        float diffW = asRect.width - (asRect.width * dmxW);
                        asRect.width = (asRect.width - diffW) * offsetScale.z;
                        asRect.x += diffW * offsetScale.x;
                        asRect.height *= offsetScale.z;
                        asRect.y += (boxRect.height - 2 - asRect.height) * (1 - offsetScale.y);
                    }

                    GUI.DrawTexture(boxRect, bgTex, ScaleMode.StretchToFill, false, aspectRatio);
                    Handles.DrawSolidRectangleWithOutline(asRect, new Color(1, 1, 1, .15f), Color.white);

                    // Y Scroll
                    Rect scrollRect = new Rect(boxRect.xMax + 5, boxRect.yMin + asRect.height / 2 - 3, 20, boxRect.height - asRect.height + 6);
                    offsetScale.y = GUI.VerticalSlider(scrollRect, !vertical || offsetScale.z < 1 ? offsetScale.y : 0, 1, 0);

                    // X Scroll
                    scrollRect = new Rect(boxRect.xMin + asRect.width / 2, boxRect.yMax + 5, boxRect.width - asRect.width, 20);
                    offsetScale.x = GUI.HorizontalSlider(scrollRect, vertical || offsetScale.z < 1 ? offsetScale.x : 0, 0, 1);

                    EditorGUI.indentLevel++;
                    Vector2 offset = EditorGUILayout.Vector2Field(new GUIContent("Offset"), new Vector2(offsetScale.x, offsetScale.y));
                    offsetScale.x = Math.Clamp(offset.x, 0, 1);
                    offsetScale.y = Math.Clamp(offset.y, 0, 1);

                    offsetScale.z = EditorGUILayout.Slider(new GUIContent("Scale"), offsetScale.z, 0, 1);
                    Rect buttonLineRect = GUILayoutUtility.GetRect(editWidth, EditorGUIUtility.singleLineHeight);
                    Rect buttonArea = EditorGUI.PrefixLabel(buttonLineRect, new GUIContent(" "));
                    Rect buttonRect = buttonArea;

                    buttonRect.width = (buttonArea.width - 10) / 3;
                    if (GUI.Button(buttonRect, new GUIContent("1080p")))
                        offsetScale.z = 1;

                    buttonRect.x = buttonArea.x + (buttonArea.width - 10) / 3 + 5;
                    if (GUI.Button(buttonRect, new GUIContent("720p")))
                        offsetScale.z = .666666f;

                    buttonRect.x = buttonArea.x + (buttonArea.width - 10) / 3 * 2 + 10;
                    if (GUI.Button(buttonRect, new GUIContent("480p")))
                        offsetScale.z = .444444f;

                    EditorGUI.indentLevel--;

                    vrslOffsetScaleProperty.vector3Value = offsetScale;

                    EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                    Rect bufferRect = GUILayoutUtility.GetRect(editWidth, EditorGUIUtility.singleLineHeight);
                    TXLGUI.DrawToggle2(bufferRect, 1, doubleBufferLabel, avproLabel, vrslDoubleBufferAVProProperty, unityLabel, vrslDoubleBufferUnityProperty);

                    //EditorGUILayout.PropertyField(vrslDoubleBufferAVProProperty, new GUIContent("Double Buffer AVPro", "Use double buffering with AVPro video sources to repeat previous frames whenever a frame is dropped."));
                }
            }

            if (vrslControllerCache)
            {
                if (vrslOutsideLinked && vrslControllerProperty.objectReferenceValue)
                    EditorGUILayout.HelpBox("VRSL controller detected in scene.  VRSL is already linked to another TXL ScreenManager.  This can cause a conflict.", MessageType.Warning, true);
                else if (vrslOutsideLinked && !vrslControllerProperty.objectReferenceValue)
                    EditorGUILayout.HelpBox("VRSL controller detected in scene.  VRSL is already linked to another TXL ScreenManager.", MessageType.Info, true);
                else if (!vrslControllerProperty.objectReferenceValue)
                    EditorGUILayout.HelpBox("VRSL controller detected in scene.  Link VRSL to have this manager keep feed video data directly to VRSL.", MessageType.Info, true);

                if (vrslControllerProperty.objectReferenceValue)
                {
                    UdonBehaviour behaviour = (UdonBehaviour)vrslControllerProperty.objectReferenceValue;
                    if (behaviour.programSource.name != "VRSL_LocalUIControlPanel")
                        EditorGUILayout.HelpBox("Specified UdonBehaviour is not a VRSL_LocalUIControlPanel script.", MessageType.Error, true);
                }
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(15);
            if (GUILayout.Button(new GUIContent("Link VRSL to this manager", "Finds VRSL in the scene and automatically configures rendering video data to it.")))
                LinkVRSL();
            GUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        private void FindExternal()
        {
            FindVRSLController();
        }

        void LinkVRSL()
        {
            TXLUdon.LinkProperty(vrslControllerProperty, FindVRSLController());
            if (vrslControllerCache)
            {
                UdonSharpBehaviour sharp = UdonSharpEditorUtility.GetProxyBehaviour(vrslControllerCache);
                if (sharp)
                {
                    int mode = (int)sharp.GetProgramVariable("DMXMode");
                    CustomRenderTexture[] crts = null;
                    if (mode == 0)
                        crts = (CustomRenderTexture[])sharp.GetProgramVariable("DMX_CRTS_Horizontal");
                    else if (mode == 1)
                        crts = (CustomRenderTexture[])sharp.GetProgramVariable("DMX_CRTS_Vertical");

                    vrslDmxRTProperty.objectReferenceValue = _FindRawRT(crts);
                }

                if (!vrslBlitMatProperty.objectReferenceValue)
                    vrslBlitMatProperty.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(DEFAULT_VRSL_MAT_PATH);

                vrslEnabledProperty.boolValue = true;
                if (vrslOffsetScaleProperty.vector3Value.z == 0)
                    vrslOffsetScaleProperty.vector3Value = new Vector3(1, 1, 1);
            }
        }

        private RenderTexture _FindRawRT(CustomRenderTexture[] crts)
        {
            if (crts == null)
                return null;

            for (int i = 0; i < crts.Length; i++)
            {
                if (!crts[i])
                    continue;

                Material mat = crts[i].material;
                if (!mat)
                    continue;

                List<string> props = new List<string>(mat.GetPropertyNames(MaterialPropertyType.Texture));
                if (!props.Contains("_DMXTexture"))
                    continue;

                Texture tex = mat.GetTexture("_DMXTexture");
                if (!tex || !(tex is RenderTexture))
                    continue;

                return (RenderTexture)tex;
            }

            return null;
        }

        private UdonBehaviour FindVRSLController()
        {
            vrslControllerCache = TXLUdon.FindExternal(vrslControllerCache, "VRSL_LocalUIControlPanel");
            return vrslControllerCache;
        }

        private bool IsVRSLOnAnotherManager()
        {
            if (managers == null)
                UpdateManagers();

            foreach (ScreenManager manager in managers)
            {
                if (manager == serializedObject.targetObject)
                    continue;

                if (manager.vrslController)
                    return true;
            }

            return false;
        }

        class CrtListDisplay : ReorderableListDisplay
        {
            ScreenManager target;
            ScreenManagerInspector inspector;

            SerializedProperty renderOutCrtListProperty;
            SerializedProperty renderOutMatPropsListProperty;
            SerializedProperty renderOutSizeListProperty;
            SerializedProperty renderOutTargetAspectListProperty;
            SerializedProperty renderOutResizeListProperty;
            SerializedProperty renderOutExpandSizeListProperty;
            SerializedProperty renderOutGlobalTexListProperty;
            SerializedProperty renderOutDoubleBufferAVProListProperty;
            SerializedProperty renderOutDoubleBufferUnityListProperty;

            static string sectionUrl = "https://vrctxl.github.io/Docs/docs/video-txl/configuration/screen-manager#render-textures";

            static GUIContent labelHeader = new GUIContent("Render Textures", "CRTs that will be enabled during video playback and recieve view or placeholder data");
            static GUIContent labelAdvancedOptions = new GUIContent("Show Advanced Options", "Show additional options that would be changed in advanced scenarios.");
            static GUIContent labelCrt = new GUIContent("CRT", "By default, a CRT has been generated in Assets for you, but you can change this for any other CRT.");
            static GUIContent labelCrtSize = new GUIContent("CRT Size", "The resolution of the CRT.  If the resize to video option is enabled, the specified size will be used for placeholder textures.\n\nChanges to this value will change the size of the underlying CRT asset.");
            static GUIContent labelTargetAspect = new GUIContent("Target Aspect Ratio", "The target aspect ratio should be set to the aspect ratio of the OBJECT that this CRT's texture will be applied to, such as a main video screen.  This can be different than the aspect ratio of the CRT or source video.\n\nIf the expand to fit option is enabled, the target aspect ratio will be used to calculate the expansion.\n\nIf the CRT material uses a compatible TXL shader, the aspect ratio property of the underlying material asset will be updated to match this value.");
            static GUIContent labelResizeVideo = new GUIContent("Resize to Video", "Dynamically resize the CRT to match the resolution of the video data.  When placeholder textures are displayed, the CRT's size specified above will be used.");
            static GUIContent labelExpandSize = new GUIContent("Enlarge to Fit", "Enlarge the dynamic size of the CRT if necessary to fit the video data within the target aspect ratio.");
            static GUIContent labelGlobalTex = new GUIContent("Set Global VideoTex", "Sets the _Udon_VideoTex global shader property with this texture.\n\n_Udon_VideoTex is used by some video players as a common property to provide a video texture to avatars.  Avoid trying to set this value from multiple video players at the same time.");
            static GUIContent labelCrtMaterial = new GUIContent("CRT Material", "The material used to render the video data onto the CRT, fetched from the underlying asset.\n\nChanges to this value will change the material on the underlying CRT asset.  Only change this if you know what you're doing.");
            static GUIContent labelCrtPropertyMap = new GUIContent("Property Map", "The property map tells the manager what property names to set on the shader used by the CRT's material.\n\nA property map is required when non-TXL shaders are used.  If you aren't using a custom CRT material, this can be left empty.");
            static GUIContent labelDoubleBuffer = new GUIContent("Double Buffered", "Whether the CRT should be double-buffered, set on the underlying asset.  If the CRT material supports a double-buffered property in the map, it will be set to match.\n\nDouble buffering can help conceal dropped video frames when using AVPro video sources, at the cost of an extra texture copy.");
            static GUIContent labelUnity = new GUIContent("Unity");
            static GUIContent labelAVPro = new GUIContent("AVPro");

            static bool expandSection = true;

            public CrtListDisplay(SerializedObject serializedObject, ScreenManager target, ScreenManagerInspector inspector) : base(serializedObject)
            {
                this.target = target;
                this.inspector = inspector;
                header = labelHeader;

                renderOutCrtListProperty = AddSerializedArray(list.serializedProperty);
                renderOutMatPropsListProperty = AddSerializedArray(serializedObject.FindProperty(nameof(ScreenManager.renderOutMatProps)));
                renderOutSizeListProperty = AddSerializedArray(serializedObject.FindProperty(nameof(ScreenManager.renderOutSize)));
                renderOutTargetAspectListProperty = AddSerializedArray(serializedObject.FindProperty(nameof(ScreenManager.renderOutTargetAspect)));
                renderOutResizeListProperty = AddSerializedArray(serializedObject.FindProperty(nameof(ScreenManager.renderOutResize)));
                renderOutExpandSizeListProperty = AddSerializedArray(serializedObject.FindProperty(nameof(ScreenManager.renderOutExpandSize)));
                renderOutGlobalTexListProperty = AddSerializedArray(serializedObject.FindProperty(nameof(ScreenManager.renderOutGlobalTex)));
                renderOutDoubleBufferAVProListProperty = AddSerializedArray(serializedObject.FindProperty(nameof(ScreenManager.renderOutDoubleBufferAVPro)));
                renderOutDoubleBufferUnityListProperty = AddSerializedArray(serializedObject.FindProperty(nameof(ScreenManager.renderOutDoubleBufferUnity)));

                list.headerHeight = 1;
            }

            protected override SerializedProperty mainListProperty(SerializedObject serializedObject)
            {
                return serializedObject.FindProperty(nameof(ScreenManager.renderOutCrt));
            }

            protected override void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
            {
                ScreenManager manager = target as ScreenManager;

                if (managers != null && !managers.Contains(manager))
                    managers.Add(manager);

                var crtProp = GetElementSafe(renderOutCrtListProperty, index);
                var matMapProp = GetElementSafe(renderOutMatPropsListProperty, index);
                var sizeProp = GetElementSafe(renderOutSizeListProperty, index);
                var targetAspectProp = GetElementSafe(renderOutTargetAspectListProperty, index);
                var resizeProp = GetElementSafe(renderOutResizeListProperty, index);
                var expandSizeProp = GetElementSafe(renderOutExpandSizeListProperty, index);
                var globalTexProp = GetElementSafe(renderOutGlobalTexListProperty, index);
                var doubleBufferAVPro = GetElementSafe(renderOutDoubleBufferAVProListProperty, index);
                var doubleBufferUnity = GetElementSafe(renderOutDoubleBufferUnityListProperty, index);

                InitRect(ref rect);

                DrawObjectField(ref rect, 0, labelCrt, crtProp);
                CustomRenderTexture crt = (CustomRenderTexture)crtProp.objectReferenceValue;
                if (!crt)
                    return;

                Vector2Int size = sizeProp.vector2IntValue;
                Vector2Int newSize = DrawSizeField(ref rect, 1, labelCrtSize, new Vector2Int(size.x > 0 ? size.x : crt.width, size.y > 0 ? size.y : crt.height));
                sizeProp.vector2IntValue = newSize;

                if (newSize.x != crt.width || newSize.y != crt.height)
                {
                    crt.Release();
                    crt.width = newSize.x;
                    crt.height = newSize.y;
                    crt.Create();
                }

                DrawFloatField(ref rect, 2, labelTargetAspect, targetAspectProp);
                DrawToggle(ref rect, 2, labelResizeVideo, resizeProp);
                if (resizeProp.boolValue)
                    DrawToggle(ref rect, 2, labelExpandSize, expandSizeProp);

                if (_showCrtAdvanced)
                {
                    Material newMat = DrawObjectField(ref rect, 1, labelCrtMaterial, crt.material, false);
                    if (newMat != crt.material)
                        crt.material = newMat;
                }

                Material mat = crt.material;
                bool compatMat = false;
                EditorScreenPropertyMap emap = null;
                List<string> propNames = new List<string>();

                if (mat != null)
                {
                    propNames.AddRange(mat.GetPropertyNames(MaterialPropertyType.Int));
                    propNames.AddRange(mat.GetPropertyNames(MaterialPropertyType.Float));

                    if (_showCrtAdvanced)
                        DrawObjectField(ref rect, 2, labelCrtPropertyMap, matMapProp);

                    // Force earlier target aspect value into material if it's a compatible TXL shader that supports it
                    compatMat = compatMaterialShader(mat);
                    if (compatMat)
                    {
                        emap = EditorScreenPropertyMap.FromPropertyMap((ScreenPropertyMap)matMapProp.objectReferenceValue);
                        if (emap == null)
                            emap = EditorScreenPropertyMap.FromMaterial(mat);

                        if (emap != null && emap.targetAspectRatio != "" && propNames.Contains(emap.targetAspectRatio))
                        {
                            float aspect = mat.GetFloat(emap.targetAspectRatio);
                            if (aspect != targetAspectProp.floatValue)
                                mat.SetFloat(emap.targetAspectRatio, targetAspectProp.floatValue);
                        }
                    }
                }

                if (_showCrtAdvanced)
                    DrawToggle2(ref rect, 1, labelDoubleBuffer, labelAVPro, doubleBufferAVPro, labelUnity, doubleBufferUnity);

                if (compatMat && emap != null && emap.doubleBuffered != "" && propNames.Contains(emap.doubleBuffered))
                    mat.SetInt(emap.doubleBuffered, crt.doubleBuffered ? 1 : 0);

                DrawToggle(ref rect, 1, labelGlobalTex, globalTexProp);
            }

            protected override float OnElementHeight(int index)
            {
                int lineCount = 1;

                var crtProp = renderOutCrtListProperty.GetArrayElementAtIndex(index);
                CustomRenderTexture crt = (CustomRenderTexture)crtProp.objectReferenceValue;
                if (crt != null)
                {
                    lineCount += 4;
                    if (_showCrtAdvanced)
                        lineCount += 2;

                    if (renderOutResizeListProperty.GetArrayElementAtIndex(index).boolValue)
                        lineCount += 1;

                    if (_showCrtAdvanced && crt.material != null)
                        lineCount += 1;
                }

                return (EditorGUIUtility.singleLineHeight + 2) * lineCount + EditorGUIUtility.singleLineHeight / 2;
            }

            protected override void OnAdd(ReorderableList list)
            {
                base.OnAdd(list);

                AddCRT(list.index, CreateCRTCopy());
            }

            void AddCRT(int index, CustomRenderTexture crt)
            {
                renderOutCrtListProperty.GetArrayElementAtIndex(index).objectReferenceValue = crt;
                renderOutSizeListProperty.GetArrayElementAtIndex(index).vector2IntValue = new Vector2Int(crt.width, crt.height);
                if (compatMaterialShader(crt))
                {
                    EditorScreenPropertyMap emap = EditorScreenPropertyMap.FromMaterial(crt.material);
                    if (emap != null && emap.targetAspectRatio != "")
                        renderOutTargetAspectListProperty.GetArrayElementAtIndex(index).floatValue = crt.material.GetFloat(emap.targetAspectRatio);
                }

                renderOutDoubleBufferAVProListProperty.GetArrayElementAtIndex(index).boolValue = true;
                renderOutGlobalTexListProperty.GetArrayElementAtIndex(index).boolValue = (index == 0);
            }

            public void AddDefault(CustomRenderTexture crt)
            {
                base.OnAdd(list);

                AddCRT(list.index, crt);
            }

            public void Section()
            {
                if (TXLEditor.DrawMainHeaderHelp(labelHeader, ref expandSection, sectionUrl))
                {
                    if (renderOutCrtListProperty.arraySize == 0)
                    {
                        bool defaultState = InDefaultState(target);
                        // If you just want to display a screen, consider using the other override options below, which will have better performance and display your video content at native resolution.
                        string info = "Due to ongoing issues with AVPro dropping video frames, a CRT setup is recommended over other update methods.";
                        if (defaultState)
                            info += "  Use the one-click button below to convert the video player's default screen setup to use a CRT.  This will generate the necessary CRT resources, as well as a shared material that will be assigned to the screen.";

                        TXLEditor.IndentedHelpBox("Custom Render Textures (CRTs), which can be used like other Render Textures, are the easiest way to supply a video texture to different materials/shaders in your scene.  This includes feeding systems like LTCGI and AreaLit.  Add one or more CRTs to the list to get started.", MessageType.Info);
                        TXLEditor.IndentedHelpBox(info, MessageType.Warning);
                        EditorGUILayout.Space();

                        if (defaultState)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(15);
                            if (GUILayout.Button("Convert to CRT Setup"))
                                inspector.ConvertDefaultToCRT();
                            
                            GUILayout.EndHorizontal();
                            EditorGUILayout.Space();
                        }
                    } else
                    {
                        EditorGUI.indentLevel += 1;
                        _showCrtAdvanced = EditorGUILayout.Toggle(labelAdvancedOptions, _showCrtAdvanced);
                        EditorGUILayout.Space();
                        EditorGUI.indentLevel -= 1;
                    }

                    Draw(1);
                    EditorGUILayout.Space(20);
                }

                if (renderOutCrtListProperty.arraySize != 0)
                {
                    TXLEditor.IndentedHelpBox("Use the CRT objects like regular textures in any of your own materials, and apply those materials to your screens or other video surfaces.", MessageType.Info);
                    EditorGUILayout.Space();
                }

                bool packageCrtDetected = false;
                for (int i = 0; i < renderOutCrtListProperty.arraySize; i++)
                {
                    SerializedProperty prop = renderOutCrtListProperty.GetArrayElementAtIndex(i);
                    CustomRenderTexture crt = (CustomRenderTexture)prop.objectReferenceValue;
                    if (crt && AssetDatabase.GetAssetPath(crt).StartsWith("Packages/com.texelsaur.video"))
                        packageCrtDetected = true;
                }

                if (packageCrtDetected)
                {
                    TXLEditor.IndentedHelpBox("One or more referenced CRTs is within the VideoTXL package folder.  The CRT will be lost or have any changes reverted the next time VideoTXL is updated.  Creating a copy in Assets is recommended.", MessageType.Warning);
                    EditorGUILayout.Space();
                }
            }
        }

        class SharedMaterialListDisplay : ReorderableListDisplay
        {
            ScreenManager target;

            SerializedProperty materialUpdateListProperty;
            SerializedProperty materialPropertyListProperty;

            static string sectionUrl = "https://vrctxl.github.io/Docs/docs/video-txl/configuration/screen-manager#shared-materials";

            static GUIContent labelHeader = new GUIContent("Shared Material Updates", "List of shared materials that will be updated as video state changes.");
            static GUIContent labelMaterial = new GUIContent("Material", "The shared material to update.");
            static GUIContent labelPropMap = new GUIContent("Property Map", "The property map tells the screen manager the names of a material's shader properties such as main texture, flipping, gamma correction, etc.\n\nProperty maps are optional if the material uses a shader supplied by VideoTXL.");
            static GUIContent labelCreatePropMap = new GUIContent("+", "Create an empty Screen Property Map object under the screen manager and asign it.  The new map will need to be filled out.");
            static GUIContent labelCreatePropMapOpt = new GUIContent("+", "Create an empty Screen Property Map object under the screen manager and asign it.  The new map will need to be filled out.\n\nThe selected material uses a shader that has a default property map, so it's not necessary to add one.");

            static bool expandSection = true;

            public SharedMaterialListDisplay(SerializedObject serializedObject, ScreenManager target) : base(serializedObject)
            {
                this.target = target;
                header = labelHeader;

                materialUpdateListProperty = AddSerializedArray(list.serializedProperty);
                materialPropertyListProperty = AddSerializedArray(serializedObject.FindProperty(nameof(ScreenManager.materialPropertyList)));

                list.headerHeight = 1;
            }

            protected override SerializedProperty mainListProperty(SerializedObject serializedObject)
            {
                return serializedObject.FindProperty(nameof(ScreenManager.materialUpdateList));
            }

            protected override void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
            {
                ScreenManager manager = target as ScreenManager;

                if (managers != null && !managers.Contains(manager))
                    managers.Add(manager);

                var matProp = GetElementSafe(materialUpdateListProperty, index);
                var mapProp = GetElementSafe(materialPropertyListProperty, index);

                InitRect(ref rect);

                DrawObjectField(ref rect, 0, labelMaterial, matProp);

                bool compatMat = materialCompat(matProp);
                Color addColor = compatMat ? GUI.backgroundColor : new Color(1, .32f, .29f);
                GUIContent label = compatMat ? labelCreatePropMapOpt : labelCreatePropMap;

                if (mapProp.objectReferenceValue != null)
                    DrawObjectField(ref rect, 1, labelPropMap, mapProp);
                else if (DrawObjectFieldWithAdd(ref rect, 1, labelPropMap, mapProp, label, EditorGUIUtility.singleLineHeight * 1, addColor))
                {
                    GameObject propMap = CreateEmptyPropertyMap(target, (Material)matProp.objectReferenceValue);
                    mapProp.objectReferenceValue = propMap.GetComponent<ScreenPropertyMap>();
                    Selection.activeObject = propMap;
                }
            }

            protected override float OnElementHeight(int index)
            {
                return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2 + EditorGUIUtility.singleLineHeight / 2;
            }

            public void Section()
            {
                if (TXLEditor.DrawMainHeaderHelp(labelHeader, ref expandSection, sectionUrl))
                {
                    Draw(1);
                    EditorGUILayout.Space(20);
                }

                int missingMapCount = MissingMaterialMapCount();
                if (missingMapCount > 0)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.HelpBox($"{missingMapCount} materials have no property map set. The screen manager will not be able to update properties on those materials.", MessageType.Error);
                    EditorGUI.indentLevel--;
                }
            }

            private int MissingMaterialMapCount()
            {
                int missingMapCount = 0;

                for (int i = 0; i < materialUpdateListProperty.arraySize; i++)
                {
                    SerializedProperty matProperties = materialPropertyListProperty.GetArrayElementAtIndex(i);
                    ScreenPropertyMap map = (ScreenPropertyMap)matProperties.objectReferenceValue;

                    SerializedProperty matProp = materialUpdateListProperty.GetArrayElementAtIndex(i);
                    Material mat = (Material)matProp.objectReferenceValue;

                    if (map == null && mat != null && !materialCompat(matProp))
                        missingMapCount += 1;
                }

                return missingMapCount;
            }
        }

        class PropBlockListDisplay : ReorderableListDisplay
        {
            enum OverrideMode
            {
                Renderer,
                Material,
            };

            ScreenManager target;

            SerializedProperty propRenderListProperty;
            SerializedProperty propMaterialOverrideListProperty;
            SerializedProperty propMaterialIndexListProperty;
            SerializedProperty propPropertyListProperty;

            static string sectionUrl = "https://vrctxl.github.io/Docs/docs/video-txl/configuration/screen-manager#material-property-blocks";

            static GUIContent labelHeader = new GUIContent("Material Property Block Overrides", "");
            static GUIContent labelRenderer = new GUIContent("Renderer", "");
            static GUIContent labelOverrideMode = new GUIContent("Override Mode", "");
            static GUIContent labelMaterialIndex = new GUIContent("Material Slot", "");
            static GUIContent labelPropMap = new GUIContent("Property Map", "");
            static GUIContent labelCreatePropMap = new GUIContent("+", "Create an empty Screen Property Map object under the screen manager and asign it.  The new map will need to be filled out.");

            static bool expandSection = true;

            public PropBlockListDisplay(SerializedObject serializedObject, ScreenManager target) : base(serializedObject)
            {
                this.target = target;
                header = labelHeader;

                propRenderListProperty = AddSerializedArray(list.serializedProperty);
                propMaterialOverrideListProperty = AddSerializedArray(serializedObject.FindProperty(nameof(ScreenManager.propMaterialOverrideList)));
                propMaterialIndexListProperty = AddSerializedArray(serializedObject.FindProperty(nameof(ScreenManager.propMaterialIndexList)));
                propPropertyListProperty = AddSerializedArray(serializedObject.FindProperty(nameof(ScreenManager.propPropertyList)));

                list.headerHeight = 1;
            }

            public void Clear()
            {
                serializedObject.FindProperty(nameof(ScreenManager.propMeshList)).ClearArray();
                serializedObject.FindProperty(nameof(ScreenManager.propMaterialOverrideList)).ClearArray();
                serializedObject.FindProperty(nameof(ScreenManager.propMaterialIndexList)).ClearArray();
                serializedObject.FindProperty(nameof(ScreenManager.propPropertyList)).ClearArray();
            }

            public MeshRenderer GetRenderer(int index)
            {
                if (index < 0 || index >= propRenderListProperty.arraySize)
                    return null;

                return (MeshRenderer)propRenderListProperty.GetArrayElementAtIndex(index).objectReferenceValue;
            }

            protected override SerializedProperty mainListProperty(SerializedObject serializedObject)
            {
                return serializedObject.FindProperty(nameof(ScreenManager.propMeshList));
            }

            protected override void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
            {
                ScreenManager manager = target as ScreenManager;

                if (managers != null && !managers.Contains(manager))
                    managers.Add(manager);

                var meshProp = GetElementSafe(propRenderListProperty, index);
                var overrideProp = GetElementSafe(propMaterialOverrideListProperty, index);
                var indexProp = GetElementSafe(propMaterialIndexListProperty, index);
                var mapProp = GetElementSafe(propPropertyListProperty, index);

                InitRect(ref rect);

                DrawObjectField(ref rect, 0, labelRenderer, meshProp);

                MeshRenderer render = (MeshRenderer)meshProp.objectReferenceValue;
                if (render)
                {
                    overrideProp.intValue = (int)DrawEnumField(ref rect, 1, labelOverrideMode, (OverrideMode)overrideProp.intValue);
                    if (overrideProp.intValue == 1)
                    {
                        string[] matEntries = GetMaterialSlots((MeshRenderer)meshProp.objectReferenceValue);
                        DrawPopupField(ref rect, 2, labelMaterialIndex, indexProp, matEntries);
                    }
                }

                bool compatMat = meshProp.objectReferenceValue == null || propOverrideCompat(manager, index);
                Color addColor = compatMat ? GUI.backgroundColor : new Color(1, .32f, .29f);

                if (mapProp.objectReferenceValue != null)
                    DrawObjectField(ref rect, 1, labelPropMap, mapProp);
                else if (DrawObjectFieldWithAdd(ref rect, 1, labelPropMap, mapProp, labelCreatePropMap, EditorGUIUtility.singleLineHeight * 1, addColor))
                {
                    GameObject propMap = CreateEmptyPropertyMap(target, null);
                    mapProp.objectReferenceValue = propMap.GetComponent<ScreenPropertyMap>();
                    Selection.activeObject = propMap;
                }
            }

            protected override float OnElementHeight(int index)
            {
                int lines = 2;

                var meshProp = GetElementSafe(propRenderListProperty, index);
                if (meshProp.objectReferenceValue != null)
                {
                    lines += 1;
                    var overrideProp = GetElementSafe(propMaterialOverrideListProperty, index);
                    if (overrideProp.intValue == 1)
                        lines += 1;
                }

                return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * lines + EditorGUIUtility.singleLineHeight / 2;
            }

            public void Section()
            {
                if (TXLEditor.DrawMainHeaderHelp(labelHeader, ref expandSection, sectionUrl))
                {
                    Draw(1);
                    EditorGUILayout.Space(20);
                }

                int missingMapCount = MissingPropMapCount();
                if (missingMapCount > 0)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.HelpBox($"{missingMapCount} override(s) have no property map set. The screen manager will not be able to update material properties on those objects.", MessageType.Error);
                    EditorGUI.indentLevel--;
                }
            }

            private int MissingPropMapCount()
            {
                int missingMapCount = 0;

                for (int i = 0; i < propRenderListProperty.arraySize; i++)
                {
                    SerializedProperty rendererProp = propRenderListProperty.GetArrayElementAtIndex(i);
                    MeshRenderer renderer = (MeshRenderer)rendererProp.objectReferenceValue;
                    if (!renderer)
                        continue;

                    SerializedProperty matProperties = propPropertyListProperty.GetArrayElementAtIndex(i);
                    ScreenPropertyMap map = (ScreenPropertyMap)matProperties.objectReferenceValue;

                    if (map == null && !propOverrideCompat(target, i))
                        missingMapCount += 1;
                }

                return missingMapCount;
            }
        }

        class ObjectOverrideListDisplay : ReorderableListDisplay
        {
            ScreenManager target;

            SerializedProperty screenMeshListProperty;
            SerializedProperty screenMatIndexListProperty;

            static GUIContent labelHeader = new GUIContent("Objects", "");
            static GUIContent labelObject = new GUIContent("Object", "");
            static GUIContent labelMaterialIndex = new GUIContent("Material Index", "");

            public ObjectOverrideListDisplay(SerializedObject serializedObject, ScreenManager target) : base(serializedObject)
            {
                this.target = target;
                header = labelHeader;

                screenMeshListProperty = AddSerializedArray(list.serializedProperty);
                screenMatIndexListProperty = AddSerializedArray(serializedObject.FindProperty(nameof(ScreenManager.screenMaterialIndex)));
            }

            protected override SerializedProperty mainListProperty(SerializedObject serializedObject)
            {
                return serializedObject.FindProperty(nameof(ScreenManager.screenMesh));
            }

            protected override void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
            {
                ScreenManager manager = target as ScreenManager;

                if (managers != null && !managers.Contains(manager))
                    managers.Add(manager);

                var meshProp = GetElementSafe(screenMeshListProperty, index);
                var matIndexProp = GetElementSafe(screenMatIndexListProperty, index);

                InitRect(ref rect);

                DrawObjectField(ref rect, 0, labelObject, meshProp);

                MeshRenderer render = (MeshRenderer)meshProp.objectReferenceValue;
                if (render)
                {
                    string[] matEntries = GetMaterialSlots(render);
                    DrawPopupField(ref rect, 1, labelMaterialIndex, matIndexProp, matEntries);
                }
            }

            protected override float OnElementHeight(int index)
            {
                int lines = 1;

                var meshProp = GetElementSafe(screenMeshListProperty, index);
                if (meshProp.objectReferenceValue != null)
                    lines += 1;

                return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * lines + EditorGUIUtility.singleLineHeight / 2;
            }

            public void Section()
            {
                Draw(1);
                EditorGUILayout.Space(20);
            }
        }

        class GlobalPropertyListDisplay : ReorderableListDisplay
        {
            ScreenManager target;

            SerializedProperty globalPropListProperty;

            static string sectionUrl = "https://vrctxl.github.io/Docs/docs/video-txl/configuration/screen-manager#global-shader-properties";

            static GUIContent labelHeader = new GUIContent("Global Property Updates", "");
            static GUIContent labelPropertyMap = new GUIContent("Property Map", "");
            static GUIContent labelCreatePropertyMap = new GUIContent("+", "Create an empty Screen Property Map object under the screen manager and asign it.  The new map will need to be filled out.");

            static bool expandSection = true;

            public GlobalPropertyListDisplay(SerializedObject serializedObject, ScreenManager target) : base(serializedObject)
            {
                this.target = target;
                header = labelHeader;

                globalPropListProperty = AddSerializedArray(list.serializedProperty);

                list.headerHeight = 1;
            }

            protected override SerializedProperty mainListProperty(SerializedObject serializedObject)
            {
                return serializedObject.FindProperty(nameof(ScreenManager.globalPropertyList));
            }

            protected override void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
            {
                InitRect(ref rect);

                SerializedProperty mapProp = GetElementSafe(globalPropListProperty, index);
                Color addColor = new Color(1, .32f, .29f);

                if (mapProp.objectReferenceValue != null)
                    DrawObjectField(ref rect, 0, labelPropertyMap, mapProp);
                else if (DrawObjectFieldWithAdd(ref rect, 0, labelPropertyMap, mapProp, labelCreatePropertyMap, EditorGUIUtility.singleLineHeight * 1, addColor))
                {
                    GameObject propMap = CreateEmptyPropertyMap(target, null);
                    mapProp.objectReferenceValue = propMap.GetComponent<ScreenPropertyMap>();
                    Selection.activeObject = propMap;
                }

                //DrawObjectField(ref rect, 0, labelPropertyMap, GetElementSafe(globalPropListProperty, index));
            }

            protected override float OnElementHeight(int index)
            {
                return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 1 + EditorGUIUtility.singleLineHeight / 4;
            }

            public void Section()
            {
                if (TXLEditor.DrawMainHeaderHelp(labelHeader, ref expandSection, sectionUrl))
                {
                    Draw(1);
                    EditorGUILayout.Space(20);
                }
            }
        }

        private static bool materialCompat(SerializedProperty matProp)
        {
            Material mat = (Material)matProp.objectReferenceValue;
            return compatMaterialShader(mat);
        }

        private static bool propOverrideCompat(ScreenManager manager, int index)
        {
            bool compat = false;

            PropBlockEntry entry = new PropBlockEntry(manager, index);
            if (!entry.renderer)
                return false;

            Material[] mats = entry.renderer.sharedMaterials;

            if (entry.useMaterialOverride)
            {
                int matIndex = manager.propMaterialIndexList[index];
                if (entry.materialIndex >= 0 && entry.materialIndex < mats.Length)
                    compat = compatMaterialShader(mats[entry.materialIndex]);
            }
            else
            {
                for (int j = 0; j < mats.Length; j++)
                {
                    if (compatMaterialShader(mats[j]))
                    {
                        compat = true;
                        break;
                    }
                }
            }

            return compat;
        }

        private static string[] GetMaterialSlots(MeshRenderer renderer)
        {
            if (!renderer)
                return new string[0];

            Material[] mats = renderer.sharedMaterials;
            string[] entries = new string[mats.Length];

            for (int i = 0; i < mats.Length; i++)
            {
                Material mat = mats[i];
                if (mat)
                    entries[i] = $"{i} - {mat.name}";
                else
                    entries[i] = $"{i} - (unassigned)";
            }

            return entries;
        }

        private static EditorScreenPropertyMap GetPropertyMapForOverride(ScreenManager manager, int index)
        {
            MeshRenderer renderer = manager.propMeshList[index];
            if (!renderer)
                return null;

            ScreenPropertyMap map = manager.propPropertyList[index];
            if (map)
                return EditorScreenPropertyMap.FromPropertyMap(map);

            Material[] mats = renderer.sharedMaterials;

            bool useMatIndex = manager.propMaterialOverrideList[index] == 1;
            if (useMatIndex)
            {
                int matIndex = manager.propMaterialIndexList[index];
                if (matIndex >= 0 && matIndex < mats.Length)
                    return EditorScreenPropertyMap.FromMaterial(mats[matIndex]);
            }
            else
            {
                for (int j = 0; j < mats.Length; j++)
                {
                    if (compatMaterialShader(mats[j]))
                        return EditorScreenPropertyMap.FromMaterial(mats[j]);
                }
            }

            return null;
        }

        private void UpdateEditorState()
        {
            ScreenManager manager = (ScreenManager)target;
            if (!target)
                return;

            UpdateEditorCRT(manager);
            UpdateEditorSharedMaterials(manager);
            UpdateEditorMaterialBlocks(manager);
            UpdateEditorVRSL(manager);
        }

        private static void UpdateEditorSharedMaterials(ScreenManager manager)
        {
            for (int i = 0; i < manager.materialUpdateList.Length; i++)
            {
                Material mat = manager.materialUpdateList[i];
                ScreenPropertyMap map = manager.materialPropertyList[i];

                if (mat)
                    UpdateSharedMaterial(manager, mat, map);
            }
        }

        private static void UpdateEditorMaterialBlocks(ScreenManager manager)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();

            TXLVideoPlayer videoPlayer = manager.videoPlayer;
            Texture2D logoTex = null;
            if (manager.editorTexture is Texture2D)
                logoTex = (Texture2D)manager.editorTexture;
            if (logoTex == null && manager.logoTexture is Texture2D)
                logoTex = (Texture2D)manager.logoTexture;

            for (int i = 0; i < manager.propMeshList.Length; i++)
            {
                bool useMatOverride = manager.propMaterialOverrideList[i] == 1;
                int matIndex = manager.propMaterialIndexList[i];

                MeshRenderer mesh = manager.propMeshList[i];
                EditorScreenPropertyMap map = GetPropertyMapForOverride(manager, i);

                if (!mesh || map == null)
                    continue;

                if (useMatOverride)
                    mesh.GetPropertyBlock(block, matIndex);
                else
                    mesh.GetPropertyBlock(block);

                if (map.screenTexture != "" && logoTex)
                    block.SetTexture(map.screenTexture, logoTex);
                if (map.avProCheck != "")
                    block.SetInt(map.avProCheck, 0);
                if (map.applyGamma != "")
                    block.SetInt(map.applyGamma, 0);
                if (map.invertY != "")
                    block.SetInt(map.invertY, 0);
                if (map.screenFit != "" && videoPlayer)
                    block.SetInt(map.screenFit, videoPlayer.screenFit);

                bool overrideAspectRatio = manager.overrideAspectRatio;
                float aspectRatio = manager.aspectRatio;
                if (map.aspectRatio != "")
                    block.SetFloat(map.aspectRatio, overrideAspectRatio && logoTex ? aspectRatio : 0);

                if (useMatOverride)
                    mesh.SetPropertyBlock(block, matIndex);
                else
                    mesh.SetPropertyBlock(block);
            }
        }

        private static void UpdateEditorCRT(ScreenManager manager)
        {
            if (!manager)
                return;

            for (int i = 0; i < manager.renderOutCrt.Length; i++)
            {
                CustomRenderTexture crt = manager.renderOutCrt[i];
                if (crt)
                {
                    Material crtMat = crt.material;
                    ScreenPropertyMap map = manager.renderOutMatProps[i];

                    UpdateSharedMaterial(manager, crtMat, map);

                    crt.Update(2);

                    if (manager.renderOutGlobalTex[i])
                    {
                        if (!videoTexRT)
                            videoTexRT = new RenderTexture(crt);
                        else
                        {
                            if (videoTexRT.IsCreated())
                                videoTexRT.Release();
                            videoTexRT.width = crt.width;
                            videoTexRT.height = crt.height;
                        }
                        videoTexRT.Create();

                        Graphics.Blit(null, videoTexRT, crtMat);
                        Shader.SetGlobalTexture("_Udon_VideoTex", videoTexRT);
                    }
                }
            }
        }

        private static void UpdateEditorVRSL(ScreenManager manager)
        {
            if (!manager || !manager.vrslDmxRT || !manager.vrslBlitMat || !vrslEditorBlitMat)
                return;

            Texture2D logoTex = null;
            if (manager.editorTexture is Texture2D)
                logoTex = (Texture2D)manager.editorTexture;
            if (logoTex == null && manager.logoTexture is Texture2D)
                logoTex = (Texture2D)manager.logoTexture;

            bool horizontal = manager.vrslDmxRT.height == 960;

            vrslEditorBlitMat.SetTexture("_MainTex", logoTex);
            vrslEditorBlitMat.SetVector("_OffsetScale", new Vector4(manager.vrslOffsetScale.x, manager.vrslOffsetScale.y, manager.vrslOffsetScale.z, manager.vrslOffsetScale.z));
            vrslEditorBlitMat.SetInt("_Horizontal", horizontal ? 1 : 0);
            vrslEditorBlitMat.SetInt("_DoubleBuffered", 0);
            vrslEditorBlitMat.SetInt("_ApplyGamma", 0);
            vrslEditorBlitMat.SetInt("_FlipY", 0);
            vrslEditorBlitMat.SetFloat("_AspectRatio", manager.vrslSourceAspectRatio);

            Graphics.Blit(logoTex, manager.vrslDmxRT, vrslEditorBlitMat);
        }

        private void UpdateSharedMaterial(Material mat, ScreenPropertyMap map)
        {
            ScreenManager manager = (ScreenManager)target;
            if (target)
                UpdateSharedMaterial(manager, mat, map);
        }

        static private void UpdateSharedMaterial(ScreenManager manager, Material mat, ScreenPropertyMap map)
        {
            EditorScreenPropertyMap emap = EditorScreenPropertyMap.FromPropertyMap(map);
            if (emap == null)
                emap = EditorScreenPropertyMap.FromMaterial(mat);

            Texture2D logoTex = null;
            if (manager.editorTexture is Texture2D)
                logoTex = (Texture2D)manager.editorTexture;
            if (logoTex == null && manager.logoTexture is Texture2D)
                logoTex = (Texture2D)manager.logoTexture;

            if (mat && emap != null)
            {
                if (emap.screenTexture != "" && logoTex)
                    mat.SetTexture(emap.screenTexture, logoTex);
                if (emap.avProCheck != "")
                    mat.SetInt(emap.avProCheck, 0);
                if (emap.applyGamma != "")
                    mat.SetInt(emap.applyGamma, 0);
                if (emap.invertY != "")
                    mat.SetInt(emap.invertY, 0);

                if (manager.videoPlayer is SyncPlayer)
                {
                    SyncPlayer videoPlayer = (SyncPlayer)manager.videoPlayer;
                    if (emap.screenFit != "" && videoPlayer)
                        mat.SetInt(emap.screenFit, (int)videoPlayer.defaultScreenFit);
                }

                bool overrideAspectRatio = manager.overrideAspectRatio;
                float aspectRatio = manager.aspectRatio;
                if (emap.aspectRatio != "")
                    mat.SetFloat(emap.aspectRatio, overrideAspectRatio && logoTex ? aspectRatio : 0);
            }
        }

        private static string GetAssetBasePath()
        {
            string destBasePath = "Assets/Texel/Generated/RenderOut";
            Scene scene = SceneManager.GetActiveScene();
            if (scene != null)
            {
                string[] parts = scene.path.Split('/');
                Array.Resize(ref parts, parts.Length - 1);
                destBasePath = $"{string.Join("/", parts)}/{scene.name}";
            }

            if (!EnsureFolderExists(destBasePath))
            {
                Debug.LogError($"Could not create folder hierarchy: {destBasePath}");
                return null;
            }

            return destBasePath;
        }

        private static CustomRenderTexture CreateCRTCopy(int id = -1)
        {
            string destBasePath = GetAssetBasePath();
            if (destBasePath == null)
                return null;

            int nextId = id;
            if (nextId == -1)
                nextId = FindNextCrtId(destBasePath);

            if (nextId < 0)
            {
                Debug.LogError("Could not find unused ID value to generate new CRT asset");
                return null;
            }

            string newMatPath = $"{destBasePath}/VideoTXLCRT-{nextId}.mat";
            if (!AssetDatabase.CopyAsset(DEFAULT_CRT_MAT_PATH, newMatPath))
            {
                Debug.LogError($"Could not copy CRT material to: {newMatPath}");
                return null;
            }

            string newCrtPath = $"{destBasePath}/VideoTXLCRT-{nextId}.asset";
            if (!AssetDatabase.CopyAsset(DEFAULT_CRT_PATH, newCrtPath))
            {
                Debug.LogError($"Could not copy CRT asset to: {newCrtPath}");
                return null;
            }

            Material mat = (Material)AssetDatabase.LoadAssetAtPath(newMatPath, typeof(Material));
            CustomRenderTexture crt = (CustomRenderTexture)AssetDatabase.LoadAssetAtPath(newCrtPath, typeof(CustomRenderTexture));

            crt.material = mat;

            return crt;
        }

        private static Material CreateStandardMaterialCopy(int id = -1)
        {
            string destBasePath = GetAssetBasePath();
            if (destBasePath == null)
                return null;

            if (id == -1)
                id = UnityEngine.Random.Range(0, 999);

            string newMatPath = $"{destBasePath}/VideoTXLScreen-{id}.mat";
            Material mat = (Material)AssetDatabase.LoadAssetAtPath(newMatPath, typeof(Material));
            if (mat)
                return mat;

            if (!AssetDatabase.CopyAsset(DEFAULT_SCREEN_MAT_PATH, newMatPath))
            {
                Debug.LogError($"Could not copy screen material to: {newMatPath}");
                return null;
            }

            return (Material)AssetDatabase.LoadAssetAtPath(newMatPath, typeof(Material));
        }

        private static bool EnsureFolderExists(string path)
        {
            string[] parts = path.Split('/');
            if (parts[0] != "Assets")
                return false;

            string subpath = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string nextfolder = $"{subpath}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(nextfolder))
                {
                    string guid = AssetDatabase.CreateFolder(subpath, parts[i]);
                    if (guid == "")
                        return false;
                }

                subpath = nextfolder;
            }

            return true;
        }

        private static int FindNextCrtId(string destBasePath)
        {
            HashSet<string> refCrts = new HashSet<string>();
            ScreenManager[] sceneManagers = FindObjectsOfType<ScreenManager>();
            foreach (var man in sceneManagers)
            {
                if (man.renderOutCrt == null)
                    continue;

                foreach (var crt in man.renderOutCrt)
                {
                    if (crt)
                        refCrts.Add(AssetDatabase.GetAssetPath(crt));
                }
            }

            for (int i = 0; i < 10; i++)
            {
                string path = $"{destBasePath}/VideoTXLCRT-{i}.asset";
                CustomRenderTexture crt = (CustomRenderTexture)AssetDatabase.LoadAssetAtPath(path, typeof(CustomRenderTexture));
                if (crt && refCrts.Contains(path))
                    continue;

                return i;
            }

            return -1;
        }

        private bool UsingNonDefaultPackageTexture()
        {
            return UsingNonDefaultPackageTexture(logoTextureProperty)
                || UsingNonDefaultPackageTexture(loadingTextureProperty)
                || UsingNonDefaultPackageTexture(audioTextureProperty)
                || UsingNonDefaultPackageTexture(errorTextureProperty)
                || UsingNonDefaultPackageTexture(errorRateLimitedTextureProperty)
                || UsingNonDefaultPackageTexture(errorInvalidTextureProperty)
                || UsingNonDefaultPackageTexture(errorRateLimitedTextureProperty)
                || UsingNonDefaultPackageTexture(editorTextureProperty);
        }

        private bool UsingNonDefaultPackageTexture(SerializedProperty prop)
        {
            if (!prop.objectReferenceValue)
                return false;

            Texture tex = (Texture)prop.objectReferenceValue;
            if (!tex)
                return false;

            string path = AssetDatabase.GetAssetPath(tex);
            if (path.StartsWith("Packages") && !ArrayUtility.Contains(PLACEHOLDER_IMAGE_PATHS, path))
                return true;

            return false;
        }

        private static bool compatMaterialShader(CustomRenderTexture crt)
        {
            if (!crt)
                return false;

            return compatMaterialShader(crt.material);
        }

        private static bool compatMaterialShader(Material mat)
        {
            if (!mat || !mat.shader)
                return false;

            switch (mat.shader.name)
            {
                case "VideoTXL/RealtimeEmissiveGamma":
                case "VideoTXL/Unlit":
                case "VideoTXL/RenderOut":
                    return true;
                default:
                    return false;
            }
        }

        private static GameObject CreateEmptyPropertyMap(ScreenManager manager, Material mat)
        {
            GameObject map = MenuUtil.AddPrefabToObject("Packages/com.texelsaur.video/Runtime/Prefabs/Component/Property Maps/PropertyMap.prefab", manager.transform);
            ScreenPropertyMap propMap = map.GetComponent<ScreenPropertyMap>();
            if (mat)
            {
                if (propMap)
                {
                    string[] texProps = mat.GetTexturePropertyNames();
                    for (int i = 0; i < texProps.Length; i++)
                    {
                        if (texProps[i] == "_MainTex" || texProps[i] == "_EmissionMap")
                        {
                            propMap.screenTexture = texProps[i];
                            break;
                        }
                    }
                }

                map.name = mat.name + " Map";
            }
            else
                map.name = "Property Map";

            return map;
        }

        private static bool InDefaultState(ScreenManager manager)
        {
            if (manager.renderOutCrt != null && manager.renderOutCrt.Length != 0)
                return false;
            if (manager.materialPropertyList != null && manager.materialPropertyList.Length != 0)
                return false;
            if (manager.propMeshList == null || manager.propMeshList.Length == 0)
                return false;

            return true;
        }

        public void ConvertDefaultToCRT()
        {
            string basePath = GetAssetBasePath();
            int crtId = FindNextCrtId(basePath);
            CustomRenderTexture crt = CreateCRTCopy(crtId);
            Material mat = CreateStandardMaterialCopy(crtId);
            mat.SetTexture("_MainTex", crt);
            mat.SetTexture("_EmissionMap", crt);

            crtList.AddDefault(crt);

            MeshRenderer mesh = propBlockList.GetRenderer(0);
            if (mesh != null)
            {
                Material[] mats = mesh.sharedMaterials;
                int matIndex = 0;

                for (int i = 0; i < mats.Length; i++)
                {
                    if (compatMaterialShader(mats[i]))
                    {
                        matIndex = i;
                        break;
                    }
                }

                mats[matIndex] = mat;
                mesh.sharedMaterials = mats;
            }

            propBlockList.Clear();
        }

        private void UpgradeLegacyCrtEntry()
        {
            bool needsUpgrade = useRenderOutProperty.boolValue && outputCRTProperty.objectReferenceValue != null && renderOutCrtListProperty.arraySize == 0;
            if (!needsUpgrade)
                return;

            CustomRenderTexture crt = (CustomRenderTexture)outputCRTProperty.objectReferenceValue;

            renderOutCrtListProperty.InsertArrayElementAtIndex(0);
            renderOutMatPropsListProperty.InsertArrayElementAtIndex(0);
            renderOutSizeListProperty.InsertArrayElementAtIndex(0);
            renderOutTargetAspectListProperty.InsertArrayElementAtIndex(0);
            renderOutResizeListProperty.InsertArrayElementAtIndex(0);
            renderOutExpandSizeListProperty.InsertArrayElementAtIndex(0);
            renderOutGlobalTexListProperty.InsertArrayElementAtIndex(0);

            SerializedProperty prop1 = renderOutCrtListProperty.GetArrayElementAtIndex(0);
            prop1.objectReferenceValue = outputCRTProperty.objectReferenceValue;

            SerializedProperty prop2 = renderOutMatPropsListProperty.GetArrayElementAtIndex(0);
            prop2.objectReferenceValue = outputMaterialPropertiesProperty.objectReferenceValue;

            SerializedProperty crtProp = GetElementSafe(renderOutCrtListProperty, 0);
            SerializedProperty matPropsProp = GetElementSafe(renderOutMatPropsListProperty, 0);
            SerializedProperty sizeProp = GetElementSafe(renderOutSizeListProperty, 0);
            SerializedProperty targetAspectProp = GetElementSafe(renderOutTargetAspectListProperty, 0);
            SerializedProperty resizeProp = GetElementSafe(renderOutResizeListProperty, 0);
            SerializedProperty expandSizeProp = GetElementSafe(renderOutExpandSizeListProperty, 0);
            SerializedProperty globalTexProp = GetElementSafe(renderOutGlobalTexListProperty, 0);

            crtProp.objectReferenceValue = crt;
            matPropsProp.objectReferenceValue = outputMaterialPropertiesProperty.objectReferenceValue;
            sizeProp.vector2IntValue = new Vector2Int(crt.width, crt.height);

            if (compatMaterialShader(crt))
                targetAspectProp.floatValue = crt.material.GetFloat("_AspectRatio");
            else
                targetAspectProp.floatValue = 0;

            resizeProp.boolValue = false;
            expandSizeProp.boolValue = false;

            outputCRTProperty.objectReferenceValue = null;
            outputMaterialPropertiesProperty.objectReferenceValue = null;
            globalTexProp.boolValue = false;
        }
    }
}