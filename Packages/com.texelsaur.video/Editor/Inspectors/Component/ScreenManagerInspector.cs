
using UnityEngine;

using UnityEditor;
using UdonSharpEditor;
using VRC.Udon;
using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

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

        SerializedProperty downloadLogoImageProperty;
        SerializedProperty downloadLogoImageUrlProperty;

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

        static bool expandTextures = true;
        static bool expandObjectOverrides = false;
        static bool expandDebug = false;
        static List<ScreenManager> managers;

        static string mainHelpUrl = "https://vrctxl.github.io/Docs/docs/video-txl/configuration/screen-manager";
        static string textureOverridesUrl = "https://vrctxl.github.io/Docs/docs/video-txl/configuration/screen-manager#texture-overrides";
        static string objectMaterialsUrl = "https://vrctxl.github.io/Docs/docs/video-txl/configuration/screen-manager#object-material-overrides";
        static string debugOptionsUrl = "https://vrctxl.github.io/Docs/docs/video-txl/configuration/screen-manager#debug-options";

        static ScreenManagerInspector()
        {
            EditorSceneManager.activeSceneChangedInEditMode += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene prevScene, Scene newScene)
        {
            ScreenManager[] found = FindObjectsOfType<ScreenManager>();

            managers = new List<ScreenManager>();
            managers.AddRange(found);

            Debug.Log($"[VideoTXL] Found {managers.Count} ScreenManagers in scene");

            UpdateEditorTextures();
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

            downloadLogoImageProperty = serializedObject.FindProperty(nameof(ScreenManager.downloadLogoImage));
            downloadLogoImageUrlProperty = serializedObject.FindProperty(nameof(ScreenManager.downloadLogoImageUrl));

            _udonSharpBackingUdonBehaviourProperty = serializedObject.FindProperty("_udonSharpBackingUdonBehaviour");

            crtList = new CrtListDisplay(serializedObject, (ScreenManager)target);
            sharedMaterialList = new SharedMaterialListDisplay(serializedObject, (ScreenManager)target);
            propBlockList = new PropBlockListDisplay(serializedObject, (ScreenManager)target);
            objectOverrideList = new ObjectOverrideListDisplay(serializedObject, (ScreenManager)target);
            globalPropList = new GlobalPropertyListDisplay(serializedObject, (ScreenManager)target);

            // CRT texture
            UpdateEditorState();

            // Upgrade legacy entries
            UpgradeLegacyCrtEntry();
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

        class CrtListDisplay : ReorderableListDisplay
        {
            ScreenManager target;

            SerializedProperty renderOutCrtListProperty;
            SerializedProperty renderOutMatPropsListProperty;
            SerializedProperty renderOutSizeListProperty;
            SerializedProperty renderOutTargetAspectListProperty;
            SerializedProperty renderOutResizeListProperty;
            SerializedProperty renderOutExpandSizeListProperty;
            SerializedProperty renderOutGlobalTexListProperty;

            static string sectionUrl = "https://vrctxl.github.io/Docs/docs/video-txl/configuration/screen-manager#render-textures";

            static GUIContent labelHeader = new GUIContent("Render Textures", "CRTs that will be enabled during video playback and recieve view or placeholder data");
            static GUIContent labelCrt = new GUIContent("CRT", "By default, a CRT has been generated in Assets for you, but you can change this for any other CRT.");
            static GUIContent labelCrtSize = new GUIContent("CRT Size", "The resolution of the CRT.  If the resize to video option is enabled, the specified size will be used for placeholder textures.\n\nChanges to this value will change the size of the underlying CRT asset.");
            static GUIContent labelTargetAspect = new GUIContent("Target Aspect Ratio", "The target aspect ratio should be set to the aspect ratio of the OBJECT that this CRT's texture will be applied to, such as a main video screen.  This can be different than the aspect ratio of the CRT or source video.\n\nIf the expand to fit option is enabled, the target aspect ratio will be used to calculate the expansion.\n\nIf the CRT material uses a compatible TXL shader, the aspect ratio property of the underlying material asset will be updated to match this value.");
            static GUIContent labelResizeVideo = new GUIContent("Resize to Video", "Dynamically resize the CRT to match the resolution of the video data.  When placeholder textures are displayed, the CRT's size specified above will be used.");
            static GUIContent labelExpandSize = new GUIContent("Enlarge to Fit", "Enlarge the dynamic size of the CRT if necessary to fit the video data within the target aspect ratio.");
            static GUIContent labelGlobalTex = new GUIContent("Set Global VideoTex", "Sets the _Udon_VideoTex global shader property with this texture.\n\n_Udon_VideoTex is used by some video players as a common property to provide a video texture to avatars.  Avoid trying to set this value from multiple video players at the same time.");
            static GUIContent labelCrtMaterial = new GUIContent("CRT Material", "The material used to render the video data onto the CRT, fetched from the underlying asset.\n\nChanges to this value will change the material on the underlying CRT asset.  Only change this if you know what you're doing.");
            static GUIContent labelCrtPropertyMap = new GUIContent("Property Map", "The property map tells the manager what property names to set on the shader used by the CRT's material.\n\nA property map is required when non-TXL shaders are used.  If you aren't using a custom CRT material, this can be left empty.");

            static bool expandSection = true;

            public CrtListDisplay(SerializedObject serializedObject, ScreenManager target) : base(serializedObject)
            {
                this.target = target;
                header = labelHeader;

                renderOutCrtListProperty = AddSerializedArray(list.serializedProperty);
                renderOutMatPropsListProperty = AddSerializedArray(serializedObject.FindProperty(nameof(ScreenManager.renderOutMatProps)));
                renderOutSizeListProperty = AddSerializedArray(serializedObject.FindProperty(nameof(ScreenManager.renderOutSize)));
                renderOutTargetAspectListProperty = AddSerializedArray(serializedObject.FindProperty(nameof(ScreenManager.renderOutTargetAspect)));
                renderOutResizeListProperty = AddSerializedArray(serializedObject.FindProperty(nameof(ScreenManager.renderOutResize)));
                renderOutExpandSizeListProperty = AddSerializedArray(serializedObject.FindProperty(nameof(ScreenManager.renderOutExpandSize)));
                renderOutGlobalTexListProperty = AddSerializedArray(serializedObject.FindProperty(nameof(ScreenManager.renderOutGlobalTex)));

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

                Material newMat = DrawObjectField(ref rect, 1, labelCrtMaterial, crt.material, false);
                if (newMat != crt.material)
                    crt.material = newMat;

                Material mat = crt.material;
                if (mat != null)
                {
                    DrawObjectField(ref rect, 2, labelCrtPropertyMap, matMapProp);

                    // Force earlier target aspect value into material if it's a compatible TXL shader that supports it
                    bool compat = compatMaterialShader(mat);
                    if (compat)
                    {
                        float aspect = mat.GetFloat("_AspectRatio");
                        if (aspect != targetAspectProp.floatValue)
                            mat.SetFloat("_AspectRatio", targetAspectProp.floatValue);
                    }
                }

                DrawToggle(ref rect, 1, labelGlobalTex, globalTexProp);
            }

            protected override float OnElementHeight(int index)
            {
                int lineCount = 1;

                var crtProp = renderOutCrtListProperty.GetArrayElementAtIndex(index);
                CustomRenderTexture crt = (CustomRenderTexture)crtProp.objectReferenceValue;
                if (crt != null)
                {
                    lineCount += 5;

                    if (renderOutResizeListProperty.GetArrayElementAtIndex(index).boolValue)
                        lineCount += 1;

                    if (crt.material != null)
                        lineCount += 1;
                }

                return (EditorGUIUtility.singleLineHeight + 2) * lineCount + EditorGUIUtility.singleLineHeight / 2;
            }

            protected override void OnAdd(ReorderableList list)
            {
                base.OnAdd(list);

                int index = list.index;
                CustomRenderTexture crt = CreateCRTCopy();

                renderOutCrtListProperty.GetArrayElementAtIndex(index).objectReferenceValue = crt;
                renderOutSizeListProperty.GetArrayElementAtIndex(index).vector2IntValue = new Vector2Int(crt.width, crt.height);
                if (compatMaterialShader(crt))
                    renderOutTargetAspectListProperty.GetArrayElementAtIndex(index).floatValue = crt.material.GetFloat("_AspectRatio");

                renderOutGlobalTexListProperty.GetArrayElementAtIndex(index).boolValue = (index == 0);
            }

            public void Section()
            {
                if (TXLEditor.DrawMainHeaderHelp(labelHeader, ref expandSection, sectionUrl))
                {
                    if (renderOutCrtListProperty.arraySize == 0)
                    {
                        TXLEditor.IndentedHelpBox("Custom Render Textures, which can be used like other Render Textures, are the easiest way to supply a video texture to different materials/shaders in your scene.  This includes feeding systems like LTCGI and AreaLit.  Add one or more CRTs to the list to get started.\n\nIf you just want to display a screen, consider using the other override options below, which will have better performance and display your video content at native resolution.", MessageType.Info);
                        EditorGUILayout.Space();
                    }

                    Draw(1);
                    EditorGUILayout.Space(20);
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
                DrawObjectField(ref rect, 0, labelPropertyMap, GetElementSafe(globalPropListProperty, index));
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
                }
            }
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

                SyncPlayer videoPlayer = (SyncPlayer)manager.videoPlayer;
                if (emap.screenFit != "" && videoPlayer)
                    mat.SetInt(emap.screenFit, (int)videoPlayer.defaultScreenFit);

                bool overrideAspectRatio = manager.overrideAspectRatio;
                float aspectRatio = manager.aspectRatio;
                if (emap.aspectRatio != "")
                    mat.SetFloat(emap.aspectRatio, overrideAspectRatio && logoTex ? aspectRatio : 0);
            }
        }

        private static CustomRenderTexture CreateCRTCopy()
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

            int nextId = FindNextCrtId(destBasePath);
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