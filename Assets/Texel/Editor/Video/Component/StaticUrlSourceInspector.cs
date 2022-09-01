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

        private void OnEnable()
        {
            multipleResolutionsProperty = serializedObject.FindProperty(nameof(StaticUrlSource.multipleResolutions));
            defaultResolutionProperty = serializedObject.FindProperty(nameof(StaticUrlSource.defaultResolution));

            staticUrlProperty = serializedObject.FindProperty(nameof(StaticUrlSource.staticUrl));
            staticUrl720Property = serializedObject.FindProperty(nameof(StaticUrlSource.staticUrl720));
            staticUrl1080Property = serializedObject.FindProperty(nameof(StaticUrlSource.staticUrl1080));
            staticUrlAudioProperty = serializedObject.FindProperty(nameof(StaticUrlSource.staticUrlAudio));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(multipleResolutionsProperty);
            if (!multipleResolutionsProperty.boolValue)
            {
                EditorGUILayout.PropertyField(staticUrlProperty);
            }
            else
            {
                int defaultResolution = EditorGUILayout.Popup("Default Resolution", defaultResolutionProperty.intValue, new string[] { "720p", "1080p", "Audio" });
                defaultResolutionProperty.intValue = defaultResolution;

                EditorGUILayout.PropertyField(staticUrl720Property);
                EditorGUILayout.PropertyField(staticUrl1080Property);
                EditorGUILayout.PropertyField(staticUrlAudioProperty);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
