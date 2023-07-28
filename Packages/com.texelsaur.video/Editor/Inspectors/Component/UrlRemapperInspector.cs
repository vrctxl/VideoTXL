using System.Collections;
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace Texel
{
    [CustomEditor(typeof(UrlRemapper))]
    public class UrlRemapperInspector : Editor
    {
        bool[] _showRuleFoldout = new bool[0];

        SerializedProperty referenceUrlsProperty;
        SerializedProperty remappedUrlsProperty;

        SerializedProperty platformRuleProperty;
        SerializedProperty sourceTypeRuleProperty;
        SerializedProperty latencyRuleProperty;
        SerializedProperty resolutionRuleProperty;
        SerializedProperty audioProfileRuleProperty;

        SerializedProperty platformsProperty;
        SerializedProperty sourceTypesProperty;
        SerializedProperty sourceLatenciesProperty;
        SerializedProperty sourceResolutionsProperty;
        SerializedProperty audioProfilesProperty;

        private void OnEnable()
        {
            referenceUrlsProperty = serializedObject.FindProperty(nameof(UrlRemapper.referenceUrls));
            remappedUrlsProperty = serializedObject.FindProperty(nameof(UrlRemapper.remappedUrls));

            platformRuleProperty = serializedObject.FindProperty(nameof(UrlRemapper.platformRule));
            sourceTypeRuleProperty = serializedObject.FindProperty(nameof(UrlRemapper.sourceTypeRule));
            latencyRuleProperty = serializedObject.FindProperty(nameof(UrlRemapper.latencyRule));
            resolutionRuleProperty = serializedObject.FindProperty(nameof(UrlRemapper.resolutionRule));
            audioProfileRuleProperty = serializedObject.FindProperty(nameof(UrlRemapper.audioProfileRule));

            platformsProperty = serializedObject.FindProperty(nameof(UrlRemapper.platforms));
            sourceTypesProperty = serializedObject.FindProperty(nameof(UrlRemapper.sourceTypes));
            sourceLatenciesProperty = serializedObject.FindProperty(nameof(UrlRemapper.sourceLatencies));
            sourceResolutionsProperty = serializedObject.FindProperty(nameof(UrlRemapper.sourceResolutions));
            audioProfilesProperty = serializedObject.FindProperty(nameof(UrlRemapper.audioProfiles));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            RuleFoldout();

            serializedObject.ApplyModifiedProperties();
        }

        private void RuleFoldout()
        {
            _showRuleFoldout = EditorTools.MultiArraySize(serializedObject, _showRuleFoldout,
                referenceUrlsProperty, remappedUrlsProperty,
                platformRuleProperty, sourceTypeRuleProperty, latencyRuleProperty, resolutionRuleProperty, audioProfileRuleProperty,
                platformsProperty, sourceTypesProperty, sourceLatenciesProperty, sourceResolutionsProperty, audioProfilesProperty);

            EditorGUILayout.Space();

            for (int i = 0; i < referenceUrlsProperty.arraySize; i++)
            {
                SerializedProperty refUrl = referenceUrlsProperty.GetArrayElementAtIndex(i);
                SerializedProperty mappedUrl = remappedUrlsProperty.GetArrayElementAtIndex(i);

                EditorGUILayout.LabelField($"Rule {i}", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(refUrl, new GUIContent("Reference URL"));
                EditorGUILayout.PropertyField(mappedUrl, new GUIContent("Remapped URL"));

                _showRuleFoldout[i] = EditorGUILayout.Foldout(_showRuleFoldout[i], $"Rules");
                if (_showRuleFoldout[i])
                {
                    EditorGUI.indentLevel++;

                    SerializedProperty platformRule = platformRuleProperty.GetArrayElementAtIndex(i);
                    SerializedProperty sourceTypeRule = sourceTypeRuleProperty.GetArrayElementAtIndex(i);
                    SerializedProperty latencyRule = latencyRuleProperty.GetArrayElementAtIndex(i);
                    SerializedProperty resolutionRule = resolutionRuleProperty.GetArrayElementAtIndex(i);
                    SerializedProperty audioProfileRule = audioProfileRuleProperty.GetArrayElementAtIndex(i);

                    SerializedProperty platform = platformsProperty.GetArrayElementAtIndex(i);
                    SerializedProperty sourceType = sourceTypesProperty.GetArrayElementAtIndex(i);
                    SerializedProperty latency = sourceLatenciesProperty.GetArrayElementAtIndex(i);
                    SerializedProperty resolution = sourceResolutionsProperty.GetArrayElementAtIndex(i);
                    SerializedProperty audioProfile = audioProfilesProperty.GetArrayElementAtIndex(i);

                    EditorGUILayout.PropertyField(platformRule, new GUIContent("Apply Platform Rule"));
                    if (platformRule.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(platform, new GUIContent("Platform Matches"));
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.PropertyField(sourceTypeRule, new GUIContent("Apply Video Source Type Rule"));
                    if (sourceTypeRule.boolValue)
                        EditorGUILayout.PropertyField(sourceType, new GUIContent("Video Source Type Matches"));

                    EditorGUILayout.PropertyField(latencyRule, new GUIContent("Apply Latency Rule"));
                    if (latencyRule.boolValue)
                        EditorGUILayout.PropertyField(latency, new GUIContent("Video Source Latency Matches"));

                    EditorGUILayout.PropertyField(resolutionRule, new GUIContent("Apply Resolution Rule"));
                    if (resolutionRule.boolValue)
                        EditorGUILayout.PropertyField(resolution, new GUIContent("Video Source Resolution Matches"));

                    EditorGUILayout.PropertyField(audioProfileRule, new GUIContent("Apply Audio Profile Rule"));
                    if (audioProfileRule.boolValue)
                        EditorGUILayout.PropertyField(audioProfile, new GUIContent("Audio Profile Matches"));

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space();
            }
        }
    }
}
