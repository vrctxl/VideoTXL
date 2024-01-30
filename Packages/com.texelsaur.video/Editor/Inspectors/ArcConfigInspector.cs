using System.Collections;
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

#if UNITY_2019
using UnityEditor.Experimental.SceneManagement;
#endif

namespace Texel
{
    [CustomEditor(typeof(ArcConfig))]
    public class ArcConfigInspector : Editor
    {
        SerializedProperty roomCenterProperty;
        SerializedProperty radiusProperty;

        private void OnEnable()
        {
            roomCenterProperty = serializedObject.FindProperty(nameof(ArcConfig.roomCenter));
            radiusProperty = serializedObject.FindProperty(nameof(ArcConfig.radius));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(roomCenterProperty, new GUIContent("Room Center", "The center of the audio space"));
            EditorGUILayout.PropertyField(radiusProperty, new GUIContent("Radius", "The distance from center to place each audio source"));

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Update Audio Sources"))
                UpdateAudioSources();
        }

        private void UpdateAudioSources()
        {
            ArcConfig target = (ArcConfig)serializedObject.targetObject;
            AudioChannelGroup group = target.GetComponent<AudioChannelGroup>();
            if (!group)
                return;

            Transform center = (Transform)roomCenterProperty.objectReferenceValue;
            if (!center)
                return;

            Vector3 offset = new Vector3(0, 0, radiusProperty.floatValue);
            Vector3 posCenter = center.position + center.rotation * offset;

            Dictionary<AudioChannelTrack, Vector3> positions = new Dictionary<AudioChannelTrack, Vector3>();
            positions[AudioChannelTrack.LEFT] = center.position + center.rotation * Quaternion.Euler(0, -35, 0) * offset;
            positions[AudioChannelTrack.RIGHT] = center.position + center.rotation * Quaternion.Euler(0, 35, 0) * offset;
            positions[AudioChannelTrack.THREE] = center.position + center.rotation * offset;
            positions[AudioChannelTrack.FOUR] = center.position + new Vector3(0, radiusProperty.floatValue / 2, 0);
            positions[AudioChannelTrack.FIVE] = center.position + center.rotation * Quaternion.Euler(0, -120, 0) * offset;
            positions[AudioChannelTrack.SIX] = center.position + center.rotation * Quaternion.Euler(0, 120, 0) * offset;

            float lcdist = (positions[AudioChannelTrack.LEFT] - positions[AudioChannelTrack.THREE]).magnitude;

            foreach (AudioChannel channel in group.avproChannels)
            {
                if (!channel || !channel.audioSourceTemplate || !positions.ContainsKey(channel.track))
                    continue;
                
                Transform audioTransform = channel.audioSourceTemplate.transform;
                audioTransform.SetPositionAndRotation(positions[channel.track] + new Vector3(0, lcdist / 2, 0), audioTransform.rotation);
                channel.audioSourceTemplate.minDistance = lcdist / 2 * 1.1f;
                channel.audioSourceTemplate.maxDistance = radiusProperty.floatValue * 3;
            }

            if (group.unityChannel)
            {
                Transform audioTransform = group.unityChannel.transform;
                audioTransform.SetPositionAndRotation(positions[AudioChannelTrack.THREE] + new Vector3(0, lcdist / 2, 0), audioTransform.rotation);
                group.unityChannel.audioSourceTemplate.minDistance = lcdist / 2 * 1.1f;
                group.unityChannel.audioSourceTemplate.maxDistance = radiusProperty.floatValue * 3;
            }

            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
                EditorSceneManager.MarkSceneDirty(stage.scene);
        }
    }
}
