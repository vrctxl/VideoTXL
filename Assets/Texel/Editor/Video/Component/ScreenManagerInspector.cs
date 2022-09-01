using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using UnityEditorInternal;
using UdonSharpEditor;

namespace Texel
{
    [CustomEditor(typeof(ScreenManager))]
    internal class ScreenManagerInspector : Editor
    {
        static bool _showErrorMatFoldout;
        static bool _showErrorTexFoldout;
        static bool _showScreenListFoldout;
        static bool[] _showScreenFoldout = new bool[0];
        static bool _showMaterialListFoldout;
        static bool[] _showMaterialFoldout = new bool[0];
        static bool[] _showMaterialOverrideFoldout = new bool[0];
        static bool _showPropListFoldout;
        static bool[] _showPropFoldout = new bool[0];
        static bool[] _showPropOverrideFoldout = new bool[0];

        SerializedProperty dataProxyProperty;

        SerializedProperty debugLogProperty;

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

        SerializedProperty videoCaptureRendererProperty;
        SerializedProperty streamCaptureRendererProperty;
        SerializedProperty captureMaterialProperty;
        SerializedProperty captureTexturePropertyProperty;
        SerializedProperty captureRTProperty;

        SerializedProperty useTextureOverrideProperty;
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
        SerializedProperty materialTexPropertyListProperty;
        SerializedProperty materialAVPropertyListProperty;

        SerializedProperty propRenderListProperty;
        SerializedProperty propMaterialOverrideListProperty;
        SerializedProperty propMaterialIndexListProperty;
        SerializedProperty propPropertyListProperty;
        SerializedProperty propMainTexListProperty;
        SerializedProperty propAVProListProperty;
        //SerializedProperty propInvertListProperty;
        //SerializedProperty propGammaListProperty;
        //SerializedProperty propFitListProperty;

        SerializedProperty useRenderOutProperty;
        SerializedProperty outputCRTProperty;
        SerializedProperty outputMaterialPropertiesProperty;

        private void OnEnable()
        {
            dataProxyProperty = serializedObject.FindProperty(nameof(ScreenManager.dataProxy));

            debugLogProperty = serializedObject.FindProperty(nameof(ScreenManager.debugLog));

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

            videoCaptureRendererProperty = serializedObject.FindProperty(nameof(ScreenManager.videoCaptureRenderer));
            streamCaptureRendererProperty = serializedObject.FindProperty(nameof(ScreenManager.streamCaptureRenderer));
            captureMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.captureMaterial));
            captureTexturePropertyProperty = serializedObject.FindProperty(nameof(ScreenManager.captureTextureProperty));
            captureRTProperty = serializedObject.FindProperty(nameof(ScreenManager.captureRT));

            useTextureOverrideProperty = serializedObject.FindProperty(nameof(ScreenManager.useTextureOverrides));
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
            materialTexPropertyListProperty = serializedObject.FindProperty(nameof(ScreenManager.materialTexPropertyList));
            materialAVPropertyListProperty = serializedObject.FindProperty(nameof(ScreenManager.materialAVPropertyList));
            materialPropertyListProperty = serializedObject.FindProperty(nameof(ScreenManager.materialPropertyList));

            propRenderListProperty = serializedObject.FindProperty(nameof(ScreenManager.propMeshList));
            propMaterialOverrideListProperty = serializedObject.FindProperty(nameof(ScreenManager.propMaterialOverrideList));
            propMaterialIndexListProperty = serializedObject.FindProperty(nameof(ScreenManager.propMaterialIndexList));
            propPropertyListProperty = serializedObject.FindProperty(nameof(ScreenManager.propPropertyList));
            propMainTexListProperty = serializedObject.FindProperty(nameof(ScreenManager.propMainTexList));
            propAVProListProperty = serializedObject.FindProperty(nameof(ScreenManager.propAVProList));
            //propInvertListProperty = serializedObject.FindProperty(nameof(ScreenManager.propInvertList));
            //propGammaListProperty = serializedObject.FindProperty(nameof(ScreenManager.propGammaList));
            //propFitListProperty = serializedObject.FindProperty(nameof(ScreenManager.propFitList));

            useRenderOutProperty = serializedObject.FindProperty(nameof(ScreenManager.useRenderOut));
            outputCRTProperty = serializedObject.FindProperty(nameof(ScreenManager.outputCRT));
            outputMaterialPropertiesProperty = serializedObject.FindProperty(nameof(ScreenManager.outputMaterialProperties));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(dataProxyProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Optional Components", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(debugLogProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Object Material Overrides", EditorStyles.boldLabel);
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
                EditorGUILayout.PropertyField(syncMaterialProperty);
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

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Material Texture Overrides", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useTextureOverrideProperty);
            if (useTextureOverrideProperty.boolValue)
            {
                EditorGUILayout.PropertyField(videoCaptureRendererProperty);
                EditorGUILayout.PropertyField(streamCaptureRendererProperty);
                //EditorGUILayout.PropertyField(captureMaterialProperty);
                //if (captureMaterialProperty.objectReferenceValue != null || videoCaptureRendererProperty.objectReferenceValue != null)
                //    EditorGUILayout.PropertyField(captureTexturePropertyProperty);
                //EditorGUILayout.PropertyField(captureRTProperty);

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(logoTextureProperty);
                EditorGUILayout.PropertyField(loadingTextureProperty);
                EditorGUILayout.PropertyField(syncTextureProperty);
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

                EditorGUILayout.Space();
                MaterialFoldout();
                EditorGUILayout.Space();
                PropBlockFoldout();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Render Texture Output", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(useRenderOutProperty);
                if (useRenderOutProperty.boolValue)
                {
                    EditorGUILayout.PropertyField(outputCRTProperty);
                    EditorGUILayout.PropertyField(outputMaterialPropertiesProperty);
                }
            }

            serializedObject.ApplyModifiedProperties();
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
            _showMaterialListFoldout = EditorGUILayout.Foldout(_showMaterialListFoldout, $"Video Screen Materials ({count})");
            if (_showMaterialListFoldout)
            {
                EditorGUI.indentLevel++;
                _showMaterialFoldout = EditorTools.MultiArraySize(serializedObject, _showMaterialFoldout,
                    materialUpdateListProperty, materialPropertyListProperty, materialTexPropertyListProperty, materialAVPropertyListProperty);

                if (_showMaterialFoldout.Length != _showMaterialOverrideFoldout.Length)
                    _showMaterialOverrideFoldout = new bool[_showMaterialFoldout.Length];

                for (int i = 0; i < materialUpdateListProperty.arraySize; i++)
                {
                    string name = EditorTools.GetMaterialName(materialUpdateListProperty, i);
                    _showMaterialFoldout[i] = EditorGUILayout.Foldout(_showMaterialFoldout[i], $"Material {i} ({name})");
                    if (_showMaterialFoldout[i])
                    {
                        EditorGUI.indentLevel++;

                        SerializedProperty matUpdate = materialUpdateListProperty.GetArrayElementAtIndex(i);
                        SerializedProperty matProperties = materialPropertyListProperty.GetArrayElementAtIndex(i);
                        SerializedProperty matTexProperty = materialTexPropertyListProperty.GetArrayElementAtIndex(i);
                        SerializedProperty matAVProperty = materialAVPropertyListProperty.GetArrayElementAtIndex(i);

                        EditorGUILayout.PropertyField(matUpdate, new GUIContent("Material"));
                        EditorGUILayout.PropertyField(matProperties, new GUIContent("Property Map"));

                        _showMaterialOverrideFoldout[i] = EditorGUILayout.Foldout(_showMaterialOverrideFoldout[i], "Property Map Overrides");
                        if (_showMaterialOverrideFoldout[i])
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.PropertyField(matTexProperty, new GUIContent("Texture Property"));
                            EditorGUILayout.PropertyField(matAVProperty, new GUIContent("AVPro Check Property"));
                            EditorGUI.indentLevel--;
                        }

                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        private void PropBlockFoldout()
        {
            int count = propRenderListProperty.arraySize;
            _showPropListFoldout = EditorGUILayout.Foldout(_showPropListFoldout, $"Material Property Block Overrides ({count})");
            if (_showPropListFoldout)
            {
                EditorGUI.indentLevel++;
                _showPropFoldout = EditorTools.MultiArraySize(serializedObject, _showPropFoldout,
                    propRenderListProperty, propMaterialOverrideListProperty, propMaterialIndexListProperty, propPropertyListProperty,
                    propMainTexListProperty, propAVProListProperty);

                if (_showPropFoldout.Length != _showPropOverrideFoldout.Length)
                    _showPropOverrideFoldout = new bool[_showPropFoldout.Length];

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
                        SerializedProperty mainTexProperty = propMainTexListProperty.GetArrayElementAtIndex(i);
                        SerializedProperty AVProProperty = propAVProListProperty.GetArrayElementAtIndex(i);
                        //SerializedProperty invertProperty = propInvertListProperty.GetArrayElementAtIndex(i);
                        //SerializedProperty gammaProperty = propGammaListProperty.GetArrayElementAtIndex(i);
                        //SerializedProperty fitProperty = propFitListProperty.GetArrayElementAtIndex(i);

                        EditorGUILayout.PropertyField(mesh, new GUIContent("Renderer"));

                        GUIContent desc = new GUIContent("Override Mode", "Whether to override a property on the renderer or one of its specific materials");
                        useMatOverride.intValue = EditorGUILayout.Popup(desc, useMatOverride.intValue, new string[] { "Renderer", "Material" });
                        if (useMatOverride.intValue == 1)
                            EditorGUILayout.PropertyField(matIndex, new GUIContent("Material Index"));

                        EditorGUILayout.PropertyField(matProperties, new GUIContent("Property Map"));

                        _showPropOverrideFoldout[i] = EditorGUILayout.Foldout(_showPropOverrideFoldout[i], "Property Map Overrides");
                        if (_showPropOverrideFoldout[i])
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.PropertyField(mainTexProperty, new GUIContent("Texture Property", "The name of the shader property holding the main screen texture"));
                            EditorGUILayout.PropertyField(AVProProperty, new GUIContent("AVPro Check Property", "Optional.  The name of the shader property that indicates the source is AVPro-based.  AVPro sources should have gamma applied for all platforms and flip the image on the Y axis on PC/VR."));
                            //EditorGUILayout.PropertyField(invertProperty, new GUIContent("Invert Y Property", "Optional.  The name of the shader property that indicates the screen should be flipped on the Y axis"));
                            //EditorGUILayout.PropertyField(gammaProperty, new GUIContent("Apply Gamma Property", "Optional.  The name of the shader property that indicates gamma correction should be applied to the image"));
                            //EditorGUILayout.PropertyField(fitProperty, new GUIContent("Screen Fit Property", "Optional.  The name of the shader property that sets the screen fit enum value (0=fit, 1=fit-h, 2=fit-w, 3=stretch)"));
                            EditorGUI.indentLevel--;
                        }

                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }
        }
    }
}