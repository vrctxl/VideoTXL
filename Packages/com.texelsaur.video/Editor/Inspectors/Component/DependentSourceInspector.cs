using System.Collections;
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace Texel
{
    [CustomEditor(typeof(DependentSource))]
    public class DependentSourceInspector : Editor
    {
        SerializedProperty primaryVideoPlayerProperty;

        DependentUrlListDisplay dependentUrlList;

        private void OnEnable()
        {
            primaryVideoPlayerProperty = serializedObject.FindProperty(nameof(DependentSource.primaryVideoPlayer));

            dependentUrlList = new DependentUrlListDisplay(serializedObject, (DependentSource)target);
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(primaryVideoPlayerProperty, new GUIContent("Primary Video Player", "The video player where the primary URL will be loaded."));
            EditorGUILayout.Space();

            dependentUrlList.Section();

            serializedObject.ApplyModifiedProperties();
        }

        static SerializedProperty GetElementSafe(SerializedProperty arr, int index)
        {
            if (arr.arraySize <= index)
                arr.arraySize = index + 1;
            return arr.GetArrayElementAtIndex(index);
        }

        class DependentUrlListDisplay : ReorderableListDisplay
        {
            DependentSource target;

            SerializedProperty primaryUrlListProperty;
            SerializedProperty dependentUrlListProperty;

            static string sectionUrl = "https://vrctxl.github.io/Docs/docs/video-txl/components/dependent-source#dependent-urls";

            static GUIContent labelHeader = new GUIContent("Dependent URLs", "List of primary/dependent URL pairs that should be loaded together.");
            static GUIContent labelPrimaryUrl = new GUIContent("Primary URL", "The main URL of the primary/dependent pair.  This URL is loaded first on the primary video player and should contain audio.");
            static GUIContent labelDependentUrl = new GUIContent("Dependent URL", "When the corresponding primary URL is loaded, the dependent URL will be loaded on the connected dependent video player after a delay, and time synced to the primary video if possible.");

            static bool expandSection = true;

            public DependentUrlListDisplay(SerializedObject serializedObject, DependentSource target) : base(serializedObject)
            {
                this.target = target;
                header = labelHeader;

                primaryUrlListProperty = AddSerializedArray(list.serializedProperty);
                dependentUrlListProperty = AddSerializedArray(serializedObject.FindProperty(nameof(DependentSource.dependentUrls)));

                list.headerHeight = 1;
            }

            protected override SerializedProperty mainListProperty(SerializedObject serializedObject)
            {
                return serializedObject.FindProperty(nameof(DependentSource.primaryUrls));
            }

            protected override void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
            {
                var primaryUrlProp = GetElementSafe(primaryUrlListProperty, index);
                var dependentUrlProp = GetElementSafe(dependentUrlListProperty, index);

                InitRect(ref rect);

                DrawPropertyField(ref rect, 0, labelPrimaryUrl, primaryUrlProp);
                DrawPropertyField(ref rect, 0, labelDependentUrl, dependentUrlProp);
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
            }
        }
    }
}
