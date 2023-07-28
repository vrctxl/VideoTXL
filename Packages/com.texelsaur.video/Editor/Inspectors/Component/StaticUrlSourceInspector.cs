using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using UnityEditorInternal;
using UdonSharpEditor;

namespace Texel
{
    [CustomEditor(typeof(StaticUrlSource))]
    public class StaticUrlSourceInspector : Editor
    {
        SerializedProperty multipleResolutionsProperty;
        SerializedProperty defaultResolutionProperty;

        SerializedProperty staticUrlProperty;
        SerializedProperty staticUrl720Property;
        SerializedProperty staticUrl1080Property;
        SerializedProperty staticUrlAudioProperty;

        SerializedProperty fallbackUrlProperty;
        SerializedProperty fallbackUrl720Property;
        SerializedProperty fallbackUrl1080Property;
        SerializedProperty fallbackUrlAudioProperty;

        SerializedProperty fallbackErrorThresholdProperty;

        private void OnEnable()
        {
            multipleResolutionsProperty = serializedObject.FindProperty(nameof(StaticUrlSource.multipleResolutions));
            defaultResolutionProperty = serializedObject.FindProperty(nameof(StaticUrlSource.defaultResolution));

            staticUrlProperty = serializedObject.FindProperty(nameof(StaticUrlSource.staticUrl));
            staticUrl720Property = serializedObject.FindProperty(nameof(StaticUrlSource.staticUrl720));
            staticUrl1080Property = serializedObject.FindProperty(nameof(StaticUrlSource.staticUrl1080));
            staticUrlAudioProperty = serializedObject.FindProperty(nameof(StaticUrlSource.staticUrlAudio));

            fallbackUrlProperty = serializedObject.FindProperty(nameof(StaticUrlSource.fallbackUrl));
            fallbackUrl720Property = serializedObject.FindProperty(nameof(StaticUrlSource.fallbackUrl720));
            fallbackUrl1080Property = serializedObject.FindProperty(nameof(StaticUrlSource.fallbackUrl1080));
            fallbackUrlAudioProperty = serializedObject.FindProperty(nameof(StaticUrlSource.fallbackUrlAudio));

            fallbackErrorThresholdProperty = serializedObject.FindProperty(nameof(StaticUrlSource.fallbackErrorThreshold));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(multipleResolutionsProperty);
            EditorGUILayout.PropertyField(fallbackErrorThresholdProperty);
            EditorGUILayout.Space();

            int errorThreshold = fallbackErrorThresholdProperty.intValue;

            if (!multipleResolutionsProperty.boolValue)
            {
                EditorGUILayout.PropertyField(staticUrlProperty);
                if (errorThreshold > 0)
                    EditorGUILayout.PropertyField(fallbackUrlProperty);
            }
            else
            {
                int defaultResolution = EditorGUILayout.Popup("Default Resolution", defaultResolutionProperty.intValue, new string[] { "720p", "1080p", "Audio" });
                defaultResolutionProperty.intValue = defaultResolution;

                EditorGUILayout.PropertyField(staticUrl720Property);
                if (errorThreshold > 0)
                {
                    EditorGUILayout.PropertyField(fallbackUrl720Property);
                    EditorGUILayout.Space();
                }

                EditorGUILayout.PropertyField(staticUrl1080Property);
                if (errorThreshold > 0)
                {
                    EditorGUILayout.PropertyField(fallbackUrl1080Property);
                    EditorGUILayout.Space();
                }

                EditorGUILayout.PropertyField(staticUrlAudioProperty);
                if (errorThreshold > 0)
                {
                    EditorGUILayout.PropertyField(fallbackUrlAudioProperty);
                    EditorGUILayout.Space();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
