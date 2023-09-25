
using UnityEngine;

using UnityEditor;
using UdonSharpEditor;
using VRC.Udon;
using System;
using System.Collections.Generic;

namespace Texel
{
    // TODO: Checks on overrides
    // [ ] Do all overrides have a mapping profile selected?
    // [ ] Do property override objects have material with CRT set?  That is probably a mixup
    // [X] Update CRT at edittime for logo?

    [CustomEditor(typeof(ScreenManager))]
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

        SerializedProperty useRenderOutProperty;
        SerializedProperty outputCRTProperty;
        SerializedProperty outputMaterialPropertiesProperty;

        SerializedProperty _udonSharpBackingUdonBehaviourProperty;

        static bool expandDebug = false;

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

            _udonSharpBackingUdonBehaviourProperty = serializedObject.FindProperty("_udonSharpBackingUdonBehaviour");

            // CRT texture
            UpdateEditorState();
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

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
                EditorGUILayout.PropertyField(aspectRatioProperty);

            EditorGUI.indentLevel--;

            // ---

            bool prevRenderOut = useRenderOutProperty.boolValue;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Render Texture Output", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(useRenderOutProperty);
            if (useRenderOutProperty.boolValue)
            {
                EditorGUILayout.PropertyField(outputCRTProperty);
                EditorGUILayout.PropertyField(outputMaterialPropertiesProperty);

                CustomRenderTexture crt = (CustomRenderTexture)outputCRTProperty.objectReferenceValue;
                if (!prevRenderOut && AssetDatabase.GetAssetPath(crt) == DEFAULT_CRT_PATH)
                    outputCRTProperty.objectReferenceValue = CreateCRTCopy();

                if (AssetDatabase.GetAssetPath(crt) == DEFAULT_CRT_PATH)
                    EditorGUILayout.HelpBox("You're using the reference CRT object from the VideoTXL package folder.  Any customization made to the CRT or its material will be lost when the VideoTXL package is updated.", MessageType.Warning);

                if (outputMaterialPropertiesProperty.objectReferenceValue == null)
                {
                    if (compatMaterialShader(crt))
                        IndentedHelpBox("Default property map inferred from compatible material shader.", MessageType.None);
                    else
                        EditorGUILayout.HelpBox($"No property map set. The screen manager will not be able to update properties on the CRT material.", MessageType.Error);
                }
            }
            else
                EditorGUILayout.HelpBox("Enabling the Render Texture Output is the easiest way to supply a video texture to other shaders and materials.  For the most control and performance, use Material or Material Property Block overrides.", MessageType.Info);

            EditorGUI.indentLevel--;

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
                                EditorGUILayout.HelpBox("No property map set. The screen manager will not be able to update properties on the material.", MessageType.Error);
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
                                EditorGUILayout.HelpBox("No property map set. The screen manager will not be able to update properties on the object.", MessageType.Error);
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

        private void UpdateEditorState()
        {
            UpdateEditorCRT();
            UpdateEditorSharedMaterials();
            UpdateEditorMaterialBlocks();
        }

        private void UpdateEditorSharedMaterials()
        {
            for (int i = 0; i < materialUpdateListProperty.arraySize; i++)
            {
                SerializedProperty matUpdate = materialUpdateListProperty.GetArrayElementAtIndex(i);
                SerializedProperty matProperties = materialPropertyListProperty.GetArrayElementAtIndex(i);

                if (matUpdate == null || matProperties == null)
                    continue;

                Material mat = (Material)matUpdate.objectReferenceValue;
                ScreenPropertyMap map = (ScreenPropertyMap)matProperties.objectReferenceValue;

                UpdateSharedMaterial(mat, map);
            }
        }

        private void UpdateEditorMaterialBlocks()
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();

            TXLVideoPlayer videoPlayer = (TXLVideoPlayer)videoPlayerProperty.objectReferenceValue;
            Texture2D logoTex = (Texture2D)editorTextureProperty.objectReferenceValue;
            if (logoTex == null)
                logoTex = (Texture2D)logoTextureProperty.objectReferenceValue;

            for (int i = 0; i < propRenderListProperty.arraySize; i++)
            {
                SerializedProperty meshProp = propRenderListProperty.GetArrayElementAtIndex(i);
                SerializedProperty useMatOverride = propMaterialOverrideListProperty.GetArrayElementAtIndex(i);
                SerializedProperty matIndex = propMaterialIndexListProperty.GetArrayElementAtIndex(i);
                SerializedProperty matProperties = propPropertyListProperty.GetArrayElementAtIndex(i);

                if (meshProp == null || matProperties == null || useMatOverride == null || matIndex == null)
                    continue;

                MeshRenderer mesh = (MeshRenderer)meshProp.objectReferenceValue;
                ScreenPropertyMap map = (ScreenPropertyMap)matProperties.objectReferenceValue;

                if (!mesh || !map)
                    continue;

                if (useMatOverride.boolValue)
                    mesh.GetPropertyBlock(block, matIndex.intValue);
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

                bool overrideAspectRatio = overrideAspectRatioProperty.boolValue;
                float aspectRatio = aspectRatioProperty.floatValue;
                if (map.aspectRatio != "")
                    block.SetFloat(map.aspectRatio, overrideAspectRatio && logoTex ? aspectRatio : 0);

                if (useMatOverride.boolValue)
                    mesh.SetPropertyBlock(block, matIndex.intValue);
                else
                    mesh.SetPropertyBlock(block);
            }
        }

        private void UpdateEditorCRT()
        {
            CustomRenderTexture crt = (CustomRenderTexture)outputCRTProperty.objectReferenceValue;
            if (crt)
            {
                Material crtMat = crt.material;
                ScreenPropertyMap map = (ScreenPropertyMap)outputMaterialPropertiesProperty.objectReferenceValue;

                UpdateSharedMaterial(crtMat, map);
            }
        }

        private void UpdateSharedMaterial(Material mat, ScreenPropertyMap map)
        {
            Texture2D logoTex = (Texture2D)editorTextureProperty.objectReferenceValue;
            if (logoTex == null)
                logoTex = (Texture2D)logoTextureProperty.objectReferenceValue;

            if (mat && map)
            {
                if (map.screenTexture != "" && logoTex)
                    mat.SetTexture(map.screenTexture, logoTex);
                if (map.avProCheck != "")
                    mat.SetInt(map.avProCheck, 0);
                if (map.applyGamma != "")
                    mat.SetInt(map.applyGamma, 0);
                if (map.invertY != "")
                    mat.SetInt(map.invertY, 0);

                SyncPlayer videoPlayer = (SyncPlayer)videoPlayerProperty.objectReferenceValue;
                if (map.screenFit != "" && videoPlayer)
                    mat.SetInt(map.screenFit, (int)videoPlayer.defaultScreenFit);

                bool overrideAspectRatio = overrideAspectRatioProperty.boolValue;
                float aspectRatio = aspectRatioProperty.floatValue;
                if (map.aspectRatio != "")
                    mat.SetFloat(map.aspectRatio, overrideAspectRatio && logoTex ? aspectRatio : 0);
            }
        }

        private CustomRenderTexture CreateCRTCopy()
        {
            string destBasePath = "Assets/Texel/Generated/RenderOut";

            if (!EnsureFolderExists(destBasePath))
            {
                Debug.LogError($"Could not create folder hierarchy: {destBasePath}");
                return null;
            }

            int nextId = FindNextCrtId();
            if (nextId < 0)
            {
                Debug.LogError("Could not find unused ID value to generate new CRT asset");
                return null;
            }

            string newMatPath = $"{destBasePath}/VideoTXLCRT.{nextId}.mat";
            if (!AssetDatabase.CopyAsset(DEFAULT_CRT_MAT_PATH, newMatPath))
            {
                Debug.LogError($"Could not copy CRT material to: {newMatPath}");
                return null;
            }

            string newCrtPath = $"{destBasePath}/VideoTXLCRT.{nextId}.asset";
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

        private int FindNextCrtId()
        {
            int id = 0;

            HashSet<string> refCrts = new HashSet<string>();
            ScreenManager[] managers = FindObjectsOfType<ScreenManager>();
            foreach (var man in managers)
            {
                if (man.outputCRT)
                    refCrts.Add(AssetDatabase.GetAssetPath(man.outputCRT));
            }

            string destBasePath = "Assets/Texel/Generated/RenderOut";
            for (int i = 0; i < 10; i++)
            {
                string path = $"{destBasePath}/VideoTXLCRT.{i}.asset";
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

        private bool compatMaterialShader(CustomRenderTexture crt)
        {
            if (!crt)
                return false;

            return compatMaterialShader(crt.material);
        }

        private bool compatMaterialShader(Material mat)
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
    }
}