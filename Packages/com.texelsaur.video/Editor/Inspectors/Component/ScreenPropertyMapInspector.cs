using System.Collections;
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace Texel
{
    [CustomEditor(typeof(ScreenPropertyMap))]
    public class ScreenPropertyMapInspector : Editor
    {
        SerializedProperty screenTextureProperty;

        SerializedProperty avProCheckProperty;
        SerializedProperty invertYProperty;
        SerializedProperty applyGammaProperty;
        SerializedProperty screenFitProperty;
        SerializedProperty aspectRatioProperty;
        SerializedProperty targetAspectRatioProperty;
        SerializedProperty doubleBufferedProperty;

        private void OnEnable()
        {
            screenTextureProperty = serializedObject.FindProperty(nameof(ScreenPropertyMap.screenTexture));

            avProCheckProperty = serializedObject.FindProperty(nameof(ScreenPropertyMap.avProCheck));
            invertYProperty = serializedObject.FindProperty(nameof(ScreenPropertyMap.invertY));
            applyGammaProperty = serializedObject.FindProperty(nameof(ScreenPropertyMap.applyGamma));
            screenFitProperty = serializedObject.FindProperty(nameof(ScreenPropertyMap.screenFit));
            aspectRatioProperty = serializedObject.FindProperty(nameof(ScreenPropertyMap.aspectRatio));
            targetAspectRatioProperty = serializedObject.FindProperty(nameof(ScreenPropertyMap.targetAspectRatio));
            doubleBufferedProperty = serializedObject.FindProperty(nameof(ScreenPropertyMap.doubleBuffered));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.HelpBox("A property map tells the screen manager the names of material's shader properties to update, such as the main (video) texture, whether the image should be flipped, etc.\n\nThe map is intended to support providing raw information to custom shaders, so not all fields need to be filled our or supported.", MessageType.Info);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(screenTextureProperty);

            EditorGUILayout.PropertyField(avProCheckProperty);
            EditorGUILayout.PropertyField(invertYProperty);
            EditorGUILayout.PropertyField(applyGammaProperty);
            EditorGUILayout.PropertyField(screenFitProperty);
            EditorGUILayout.PropertyField(aspectRatioProperty);
            EditorGUILayout.PropertyField(targetAspectRatioProperty);
            EditorGUILayout.PropertyField(doubleBufferedProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
