
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
        };

        static bool _showErrorMatFoldout;
        static bool _showErrorTexFoldout;
        static bool _showScreenListFoldout;
        static bool[] _showScreenFoldout = new bool[0];
        static bool _showMaterialListFoldout;
        static bool[] _showMaterialFoldout = new bool[0];
        static bool _showPropListFoldout;
        static bool[] _showPropFoldout = new bool[0];

        SerializedProperty videoPlayerProperty;

        SerializedProperty debugLogProperty;
        SerializedProperty vrcLoggingProperty;
        SerializedProperty lowLevelLoggingProperty;
        SerializedProperty eventLoggingProperty;

        SerializedProperty useMaterialOverrideProperty;
        SerializedProperty separatePlaybackMaterialsProperty;
        SerializedProperty playbackMaterialProperty;
        SerializedProperty playbackMaterialUnityProperty;
        SerializedProperty playbackMaterialAVProProperty;
        SerializedProperty logoMaterialProperty;
        SerializedProperty loadingMaterialProperty;
        SerializedProperty syncMaterialProperty;
        SerializedProperty audioMaterialProperty;
        SerializedProperty errorMaterialProperty;
        SerializedProperty errorInvalidMaterialProperty;
        SerializedProperty errorBlockedMaterialProperty;
        SerializedProperty errorRateLimitedMaterialProperty;
        SerializedProperty editorMaterialProperty;

        SerializedProperty screenMeshListProperty;
        SerializedProperty screenMatIndexListProperty;

        SerializedProperty useTextureOverrideProperty;
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

        SerializedProperty materialUpdateListProperty;
        SerializedProperty materialPropertyListProperty;

        SerializedProperty propRenderListProperty;
        SerializedProperty propMaterialOverrideListProperty;
        SerializedProperty propMaterialIndexListProperty;
        SerializedProperty propPropertyListProperty;

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

        ReorderableList crtOutList;

        static bool expandDebug = false;
        static List<ScreenManager> managers;

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
            vrcLoggingProperty = serializedObject.FindProperty(nameof(ScreenManager.vrcLogging));
            lowLevelLoggingProperty = serializedObject.FindProperty(nameof(ScreenManager.lowLevelLogging));
            eventLoggingProperty = serializedObject.FindProperty(nameof(ScreenManager.eventLogging));

            useMaterialOverrideProperty = serializedObject.FindProperty(nameof(ScreenManager.useMaterialOverrides));
            separatePlaybackMaterialsProperty = serializedObject.FindProperty(nameof(ScreenManager.separatePlaybackMaterials));
            playbackMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.playbackMaterial));
            playbackMaterialUnityProperty = serializedObject.FindProperty(nameof(ScreenManager.playbackMaterialUnity));
            playbackMaterialAVProProperty = serializedObject.FindProperty(nameof(ScreenManager.playbackMaterialAVPro));
            logoMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.logoMaterial));
            loadingMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.loadingMaterial));
            syncMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.syncMaterial));
            audioMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.audioMaterial));
            errorMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.errorMaterial));
            errorInvalidMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.errorInvalidMaterial));
            errorBlockedMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.errorBlockedMaterial));
            errorRateLimitedMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.errorRateLimitedMaterial));
            editorMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.editorMaterial));

            screenMeshListProperty = serializedObject.FindProperty(nameof(ScreenManager.screenMesh));
            screenMatIndexListProperty = serializedObject.FindProperty(nameof(ScreenManager.screenMaterialIndex));

            useTextureOverrideProperty = serializedObject.FindProperty(nameof(ScreenManager.useTextureOverrides));
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

            materialUpdateListProperty = serializedObject.FindProperty(nameof(ScreenManager.materialUpdateList));
            materialPropertyListProperty = serializedObject.FindProperty(nameof(ScreenManager.materialPropertyList));

            propRenderListProperty = serializedObject.FindProperty(nameof(ScreenManager.propMeshList));
            propMaterialOverrideListProperty = serializedObject.FindProperty(nameof(ScreenManager.propMaterialOverrideList));
            propMaterialIndexListProperty = serializedObject.FindProperty(nameof(ScreenManager.propMaterialIndexList));
            propPropertyListProperty = serializedObject.FindProperty(nameof(ScreenManager.propPropertyList));

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

            crtOutList = new ReorderableList(serializedObject, renderOutCrtListProperty);
            crtOutList.drawElementCallback = OnDrawElement;
            crtOutList.drawHeaderCallback = OnDrawHeader;
            crtOutList.onAddCallback = OnAdd;
            crtOutList.onRemoveCallback = OnRemove;
            crtOutList.elementHeightCallback = OnElementHeight;
            crtOutList.footerHeight = -15;
            crtOutList.draggable = false;

            // CRT texture
            UpdateEditorState();
        }

        SerializedProperty GetElementSafe(SerializedProperty arr, int index)
        {
            if (arr.arraySize <= index)
                arr.arraySize = index + 1;
            return arr.GetArrayElementAtIndex(index);
        }

        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
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

            float lineHeight = EditorGUIUtility.singleLineHeight;
            rect.height = lineHeight;

            rect.y += EditorGUIUtility.standardVerticalSpacing;
            //EditorGUI.LabelField(rect, $"Output CRT {index}");

            //rect.y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(rect, crtProp, new GUIContent("CRT", "By default, a CRT has been generated in Assets for you, but you can change this for any other CRT."));

            CustomRenderTexture crt = (CustomRenderTexture)crtProp.objectReferenceValue;
            if (crt != null)
            {
                rect.x += 15;
                rect.y += lineHeight + EditorGUIUtility.standardVerticalSpacing;

                Vector2Int size = sizeProp.vector2IntValue;

                Rect fieldRect = EditorGUI.PrefixLabel(rect, new GUIContent("CRT Size", "The resolution of the CRT.  If the resize to video option is enabled, the specified size will be used for placeholder textures.\n\nChanges to this value will change the size of the underlying CRT asset."));
                fieldRect.x -= 15;
                Rect field1 = fieldRect;
                float xwidth = 8;
                float xpad = 5;
                float fwidth = (fieldRect.width - xwidth - xpad * 2) / 2;

                field1.width = fwidth;
                int width = EditorGUI.DelayedIntField(field1, size.x > 0 ? size.x : crt.width);
                field1.x += fwidth + xpad;
                field1.width = xwidth;
                EditorGUI.LabelField(field1, "x");

                field1.x += xwidth + xpad;
                field1.width = fwidth;
                int height = EditorGUI.DelayedIntField(field1, size.y > 0 ? size.y : crt.height);

                if (width != crt.width || height != crt.height)
                {
                    crt.Release();
                    crt.width = width;
                    crt.height = height;
                    crt.Create();
                }

                sizeProp.vector2IntValue = new Vector2Int(width, height);

                rect.x += 15;
                rect.y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
                fieldRect = EditorGUI.PrefixLabel(rect, new GUIContent("Target Aspect Ratio", "The target aspect ratio should be set to the aspect ratio of the OBJECT that this CRT's texture will be applied to, such as a main video screen.  This can be different than the aspect ratio of the CRT or source video.\n\nIf the expand to fit option is enabled, the target aspect ratio will be used to calculate the expansion.\n\nIf the CRT material uses a compatible TXL shader, the aspect ratio property of the underlying material asset will be updated to match this value."));
                fieldRect.x -= 30;
                fieldRect.width = fwidth;
                float newAspect = EditorGUI.DelayedFloatField(fieldRect, targetAspectProp.floatValue);
                targetAspectProp.floatValue = newAspect;

                rect.y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
                fieldRect = EditorGUI.PrefixLabel(rect, new GUIContent("Resize to Video", "Dynamically resize the CRT to match the resolution of the video data.  When placeholder textures are displayed, the CRT's size specified above will be used."));
                fieldRect.x -= 30;
                resizeProp.boolValue = EditorGUI.Toggle(fieldRect, resizeProp.boolValue);

                if (resizeProp.boolValue)
                {
                    rect.y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
                    fieldRect = EditorGUI.PrefixLabel(rect, new GUIContent("Enlarge to Fit", "Enlarge the dynamic size of the CRT if necessary to fit the video data within the target aspect ratio."));
                    fieldRect.x -= 30;
                    expandSizeProp.boolValue = EditorGUI.Toggle(fieldRect, expandSizeProp.boolValue);
                }
                rect.x -= 15;

                Material mat = crt.material;
                rect.y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
                fieldRect = EditorGUI.PrefixLabel(rect, new GUIContent("CRT Material", "The material used to render the video data onto the CRT, fetched from the underlying asset.\n\nChanges to this value will change the material on the underlying CRT asset.  Only change this if you know what you're doing."));
                fieldRect.x -= 15;
                Material newMat = (Material)EditorGUI.ObjectField(fieldRect, mat, typeof(Material), false);
                if (newMat != mat)
                {
                    crt.material = newMat;
                    mat = newMat;
                }

                if (mat != null)
                {
                    ScreenPropertyMap matmap = (ScreenPropertyMap)matMapProp.objectReferenceValue;
                    rect.x += 15;
                    rect.y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
                    fieldRect = EditorGUI.PrefixLabel(rect, new GUIContent("Property Map", "The property map tells the manager what property names to set on the shader used by the CRT's material.\n\nA property map is required when non-TXL shaders are used.  If you aren't using a custom CRT material, this can be left empty."));
                    fieldRect.x -= 30;
                    EditorGUI.ObjectField(fieldRect, matMapProp, GUIContent.none);

                    // Force earlier target aspect value into material if it's a compatible TXL shader that supports it
                    bool compat = compatMaterialShader(mat);
                    if (compat)
                    {
                        float aspect = mat.GetFloat("_AspectRatio");
                        if (aspect != newAspect)
                            mat.SetFloat("_AspectRatio", newAspect);
                    }

                    rect.x -= 15;
                }

                rect.y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
                fieldRect = EditorGUI.PrefixLabel(rect, new GUIContent("Set Global VideoTex", "Sets the _Udon_VideoTex global shader property with this texture.\n\n_Udon_VideoTex is used by some video players as a common property to provide a video texture to avatars.  Avoid trying to set this value from multiple video players at the same time."));
                fieldRect.x -= 15;
                globalTexProp.boolValue = EditorGUI.Toggle(fieldRect, globalTexProp.boolValue);

                rect.x -= 15;
            }
        }

        private void OnDrawHeader(Rect rect)
        {
            GUI.Label(rect, new GUIContent("Output Custom Render Textures (CRTs)", "CRTs that will be enabled during video playback and recieve view or placeholder data"));
        }

        private void OnAdd(ReorderableList list)
        {
            int index = AddElement(renderOutCrtListProperty, renderOutMatPropsListProperty, renderOutSizeListProperty, renderOutTargetAspectListProperty,
                renderOutResizeListProperty, renderOutExpandSizeListProperty, renderOutGlobalTexListProperty);

            list.index = index;
            CustomRenderTexture crt = CreateCRTCopy();

            renderOutCrtListProperty.GetArrayElementAtIndex(index).objectReferenceValue = crt;
            renderOutSizeListProperty.GetArrayElementAtIndex(index).vector2IntValue = new Vector2Int(crt.width, crt.height);
            if (compatMaterialShader(crt))
                renderOutTargetAspectListProperty.GetArrayElementAtIndex(index).floatValue = crt.material.GetFloat("_AspectRatio");

            renderOutGlobalTexListProperty.GetArrayElementAtIndex(index).boolValue = (index == 0);

        }

        private void OnRemove(ReorderableList list)
        {
            if (list.index < 0 || list.index >= list.count)
                return;

            RemoveElement(renderOutCrtListProperty, renderOutMatPropsListProperty, renderOutSizeListProperty, renderOutTargetAspectListProperty,
                renderOutResizeListProperty, renderOutExpandSizeListProperty, renderOutGlobalTexListProperty);

            if (list.index == renderOutCrtListProperty.arraySize)
                list.index--;
        }

        private float OnElementHeight(int index)
        {
            int lineCount = 2;

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

            return (EditorGUIUtility.singleLineHeight + 2) * lineCount;
        }

        private int AddElement(SerializedProperty main, params SerializedProperty[] props)
        {
            int index = main.arraySize;

            main.arraySize++;
            foreach (var prop in props)
                prop.arraySize++;

            return index;
        }

        private void RemoveElement(params SerializedProperty[] props)
        {
            foreach (var prop in props)
                prop.arraySize--;
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            if (useRenderOutProperty.boolValue && outputCRTProperty.objectReferenceValue != null && renderOutCrtListProperty.arraySize == 0)
            {
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

            GUIStyle boldFoldoutStyle = new GUIStyle(EditorStyles.foldout);
            boldFoldoutStyle.fontStyle = FontStyle.Bold;

            if (GUILayout.Button("Screen Manager Documentation"))
                Application.OpenURL("https://github.com/jaquadro/VideoTXL/wiki/Configuration:-Screen-Manager");

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(videoPlayerProperty);

            // ---

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Texture Overrides", EditorStyles.boldLabel);

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

            // ---

            bool prevRenderOut = useRenderOutProperty.boolValue;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Render Texture Output", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            if (renderOutCrtListProperty.arraySize == 0)
                EditorGUILayout.HelpBox("Custom Render Textures, which can be used like other Render Textures, are the easiest way to supply a video texture to different materials/shaders in your scene.  This includes feeding systems like LTCGI and AreaLit.  Add one or more CRTs to the list to get started.\n\nIf you just want to display a screen, consider using the other override options below, which will have better performance and display your video content at native resolution.", MessageType.Info);
            EditorGUI.indentLevel--;

            Rect listRect = GUILayoutUtility.GetRect(0, crtOutList.GetHeight() + 16, GUILayout.ExpandWidth(true));
            listRect.x += 15;
            listRect.width -= 15;
            crtOutList.DoList(listRect);

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
                EditorGUILayout.Space(20);
                IndentedHelpBox("One or more referenced CRTs is within the VideoTXL package folder.  The CRT will be lost or have any changes reverted the next time VideoTXL is updated.  Creating a copy in Assets is recommended.", MessageType.Warning);
            }

            // ---

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Other Texture Overrides", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            EditorGUILayout.Space();
            MaterialFoldout();
            EditorGUILayout.Space();
            PropBlockFoldout();
            EditorGUI.indentLevel--;

            // ---

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Object Material Overrides", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(useMaterialOverrideProperty);
            if (useMaterialOverrideProperty.boolValue)
            {
                EditorGUILayout.PropertyField(separatePlaybackMaterialsProperty);
                if (separatePlaybackMaterialsProperty.boolValue)
                {
                    EditorGUILayout.PropertyField(playbackMaterialUnityProperty);
                    EditorGUILayout.PropertyField(playbackMaterialAVProProperty);
                }
                else
                    EditorGUILayout.PropertyField(playbackMaterialProperty);

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

                EditorGUILayout.PropertyField(editorMaterialProperty);

                EditorGUILayout.Space();
                ScreenFoldout();
            }

            EditorGUI.indentLevel--;

            // ---

            EditorGUILayout.Space();
            expandDebug = EditorGUILayout.Foldout(expandDebug, "Debug Options", true, boldFoldoutStyle);
            if (expandDebug)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(debugLogProperty, new GUIContent("Debug Log", "Log debug statements to a world object"));
                EditorGUILayout.PropertyField(eventLoggingProperty, new GUIContent("Include Events", "Include additional event traffic in debug log"));
                EditorGUILayout.PropertyField(lowLevelLoggingProperty, new GUIContent("Include Low Level", "Include additional verbose messages in debug log"));
                EditorGUILayout.PropertyField(vrcLoggingProperty, new GUIContent("VRC Logging", "Write out debug messages to VRChat log."));
                EditorGUI.indentLevel--;
            }

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
                UpdateEditorState();
            }
        }

        private void IndentedHelpBox(string message, MessageType messageType)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox(message, messageType);
            EditorGUI.indentLevel--;
        }

        private void ScreenFoldout()
        {
            int count = screenMeshListProperty.arraySize;
            _showScreenListFoldout = EditorGUILayout.Foldout(_showScreenListFoldout, $"Video Screen Objects ({count})");
            if (_showScreenListFoldout)
            {
                EditorGUI.indentLevel++;
                _showScreenFoldout = EditorTools.MultiArraySize(serializedObject, _showScreenFoldout,
                    screenMeshListProperty, screenMatIndexListProperty);
                
                for (int i = 0; i < screenMeshListProperty.arraySize; i++)
                {
                    string name = EditorTools.GetMeshRendererName(screenMeshListProperty, i);
                    _showScreenFoldout[i] = EditorGUILayout.Foldout(_showScreenFoldout[i], $"Screen {i} ({name})");
                    if (_showScreenFoldout[i])
                    {
                        EditorGUI.indentLevel++;

                        SerializedProperty mesh = screenMeshListProperty.GetArrayElementAtIndex(i);
                        SerializedProperty matIndex = screenMatIndexListProperty.GetArrayElementAtIndex(i);

                        EditorGUILayout.PropertyField(mesh, new GUIContent("Mesh Renderer"));
                        EditorGUILayout.PropertyField(matIndex, new GUIContent("Material Index"));

                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        private void MaterialFoldout()
        {
            int count = materialUpdateListProperty.arraySize;
            _showMaterialListFoldout = EditorGUILayout.Foldout(_showMaterialListFoldout, $"Shared Material Updates ({count})");
            if (_showMaterialListFoldout)
            {
                EditorGUI.indentLevel++;
                _showMaterialFoldout = EditorTools.MultiArraySize(serializedObject, _showMaterialFoldout,
                    materialUpdateListProperty, materialPropertyListProperty);

                for (int i = 0; i < materialUpdateListProperty.arraySize; i++)
                {
                    string name = EditorTools.GetMaterialName(materialUpdateListProperty, i);
                    _showMaterialFoldout[i] = EditorGUILayout.Foldout(_showMaterialFoldout[i], $"Material {i} ({name})");
                    if (_showMaterialFoldout[i])
                    {
                        EditorGUI.indentLevel++;

                        SerializedProperty matUpdate = materialUpdateListProperty.GetArrayElementAtIndex(i);
                        SerializedProperty matProperties = materialPropertyListProperty.GetArrayElementAtIndex(i);

                        EditorGUILayout.PropertyField(matUpdate, new GUIContent("Material"));
                        EditorGUILayout.PropertyField(matProperties, new GUIContent("Property Map"));

                        if (!objectRefValid(materialPropertyListProperty, i)) {
                            if (materialCompat(i))
                                IndentedHelpBox("Default property map inferred from compatible material shader.", MessageType.None);
                            else if (objectRefValid(materialUpdateListProperty, i))
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button(new GUIContent("Create Property Map", "Create an empty Screen Property Map object under the screen manager and asign it.  The new map will need to be filled out."), GUILayout.Width(150)))
                                {
                                    GameObject propMap = CreateEmptyPropertyMap((Material)matUpdate.objectReferenceValue);
                                    matProperties.objectReferenceValue = propMap.GetComponent<ScreenPropertyMap>();
                                    Selection.activeObject = propMap;
                                }
                                GUILayout.EndHorizontal();
                                EditorGUILayout.HelpBox("No property map set.  The screen manager will not be able to update properties on the material.  A property map tells the screen manager the names of a material's shader properties such as main texture, flipping, gamma correction, etc.", MessageType.Error);
                            }
                        } 

                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }

            int missingMapCount = MissingMaterialMapCount();
            if (missingMapCount > 0)
                EditorGUILayout.HelpBox($"{missingMapCount} materials have no property map set. The screen manager will not be able to update properties on those materials.", MessageType.Error);
        }

        private void PropBlockFoldout()
        {
            int count = propRenderListProperty.arraySize;
            _showPropListFoldout = EditorGUILayout.Foldout(_showPropListFoldout, $"Material Property Block Overrides ({count})");
            if (_showPropListFoldout)
            {
                EditorGUI.indentLevel++;
                _showPropFoldout = EditorTools.MultiArraySize(serializedObject, _showPropFoldout,
                    propRenderListProperty, propMaterialOverrideListProperty, propMaterialIndexListProperty, propPropertyListProperty);

                for (int i = 0; i < propRenderListProperty.arraySize; i++)
                {
                    string name = EditorTools.GetMeshRendererName(propRenderListProperty, i);
                    _showPropFoldout[i] = EditorGUILayout.Foldout(_showPropFoldout[i], $"Material Override {i} ({name})");
                    if (_showPropFoldout[i])
                    {
                        EditorGUI.indentLevel++;

                        SerializedProperty mesh = propRenderListProperty.GetArrayElementAtIndex(i);
                        SerializedProperty useMatOverride = propMaterialOverrideListProperty.GetArrayElementAtIndex(i);
                        SerializedProperty matIndex = propMaterialIndexListProperty.GetArrayElementAtIndex(i);
                        SerializedProperty matProperties = propPropertyListProperty.GetArrayElementAtIndex(i);

                        EditorGUILayout.PropertyField(mesh, new GUIContent("Renderer"));

                        GUIContent desc = new GUIContent("Override Mode", "Whether to override a property on the renderer or one of its specific materials");
                        useMatOverride.intValue = EditorGUILayout.Popup(desc, useMatOverride.intValue, new string[] { "Renderer", "Material" });
                        if (useMatOverride.intValue == 1)
                            EditorGUILayout.PropertyField(matIndex, new GUIContent("Material Index"));

                        EditorGUILayout.PropertyField(matProperties, new GUIContent("Property Map"));

                        if (!objectRefValid(propPropertyListProperty, i))
                        {
                            if (propOverrideCompat(i))
                                IndentedHelpBox("Default property map inferred from compatible material shader.", MessageType.None);
                            else if (objectRefValid(propRenderListProperty, i))
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button(new GUIContent("Create Property Map", "Create an empty Screen Property Map object under the screen manager and asign it.  The new map will need to be filled out."), GUILayout.Width(150)))
                                {
                                    GameObject propMap = CreateEmptyPropertyMap(null);
                                    matProperties.objectReferenceValue = propMap.GetComponent<ScreenPropertyMap>();
                                    Selection.activeObject = propMap;
                                }
                                GUILayout.EndHorizontal();
                                EditorGUILayout.HelpBox("No property map set. The screen manager will not be able to update properties on the object.  A property map tells the screen manager the names of a material's shader properties such as main texture, flipping, gamma correction, etc.", MessageType.Error);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }

            int missingMapCount = MissingPropMapCount();
            if (missingMapCount > 0)
                EditorGUILayout.HelpBox($"{missingMapCount} override(s) have no property map set. The screen manager will not be able to update material properties on those objects.", MessageType.Error);
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

                if (map == null && mat != null && !materialCompat(i))
                    missingMapCount += 1;
            }

            return missingMapCount;
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

                if (map == null && !propOverrideCompat(i))
                    missingMapCount += 1;
            }

            return missingMapCount;
        }

        private bool objectRefValid(SerializedProperty propList, int index)
        {
            SerializedProperty matProperties = propList.GetArrayElementAtIndex(index);
            return matProperties.objectReferenceValue != null;
        }

        private bool materialCompat(int index)
        {
            SerializedProperty matProp = materialUpdateListProperty.GetArrayElementAtIndex(index);
            Material mat = (Material)matProp.objectReferenceValue;

            return compatMaterialShader(mat);
        }

        private Material materialOverride(int index)
        {
            SerializedProperty matProp = materialUpdateListProperty.GetArrayElementAtIndex(index);
            Material mat = (Material)matProp.objectReferenceValue;

            return mat;
        }

        private bool propOverrideCompat(int index)
        {
            bool compat = false;

            SerializedProperty rendererProp = propRenderListProperty.GetArrayElementAtIndex(index);
            MeshRenderer renderer = (MeshRenderer)rendererProp.objectReferenceValue;
            if (!renderer)
                return false;

            Material[] mats = renderer.sharedMaterials;

            bool useMatIndex = propMaterialOverrideListProperty.GetArrayElementAtIndex(index).intValue == 1;
            if (useMatIndex)
            {
                int matIndex = propMaterialIndexListProperty.GetArrayElementAtIndex(index).intValue;
                if (matIndex >= 0 && matIndex < mats.Length)
                    compat = compatMaterialShader(mats[matIndex]);
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

        private EditorScreenPropertyMap GetPropertyMapForOverride(int index)
        {
            ScreenManager manager = (ScreenManager)target;
            if (target)
                return GetPropertyMapForOverride(manager, index);

            return null;
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
            Texture2D logoTex = (Texture2D)manager.editorTexture;
            if (logoTex == null)
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

            Texture2D logoTex = (Texture2D)manager.editorTexture;
            if (logoTex == null)
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

        private CustomRenderTexture CreateCRTCopy()
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

        private bool EnsureFolderExists(string path)
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

        private int FindNextCrtId(string destBasePath)
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
                case "VideoTXL/RenderOut":
                    return true;
                default:
                    return false;
            }
        }

        private GameObject CreateEmptyPropertyMap(Material mat)
        {
            ScreenManager man = (ScreenManager)serializedObject.targetObject;

            GameObject map = VideoTxlManager.AddPrefabToObject("Packages/com.texelsaur.video/Runtime/Prefabs/Component/Property Maps/PropertyMap.prefab", man.transform);
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
    }
}