using System.Collections;
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace Texel
{
    [CustomEditor(typeof(AccessControlUserList))]
    public class AccessControlUserListInspector : Editor
    {
        SerializedProperty userListProperty;

        string bulkText = "";
        // static bool showUserFoldout = true;

        private void OnEnable()
        {
            userListProperty = serializedObject.FindProperty(nameof(AccessControlUserList.userList));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Bulk Operations", EditorStyles.boldLabel);

            bulkText = EditorGUILayout.TextArea(bulkText, GUILayout.Height(70));
            Rect row = EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Add Names", "Add each name from newline-separated list to the user list if the name is not already present")))
            {
                AppendNames(bulkText);
                bulkText = "";
                EditorUtility.SetDirty(target);
            }
            if (GUILayout.Button(new GUIContent("Replace Existing", "Clears existing user list and adds each name from newline-separated list")))
            {
                ClearList();
                AppendNames(bulkText);
                bulkText = "";
                EditorUtility.SetDirty(target);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("User List", EditorStyles.boldLabel);

            //showUserFoldout = EditorGUILayout.Foldout(showUserFoldout, new GUIContent("User List"));
            //if (showUserFoldout)
            //{
                EditorGUI.indentLevel++;

                int oldCount = userListProperty.arraySize;
                int newCount = Mathf.Max(0, EditorGUILayout.DelayedIntField("Size", userListProperty.arraySize));
                if (newCount != oldCount)
                {
                    for (int i = oldCount; i < newCount; i++)
                    {
                        userListProperty.InsertArrayElementAtIndex(i);
                        SerializedProperty prop = userListProperty.GetArrayElementAtIndex(i);
                        prop.stringValue = "";
                    }

                    serializedObject.ApplyModifiedProperties();
                }

                for (int i = 0; i < userListProperty.arraySize; i++)
                {
                    SerializedProperty prop = userListProperty.GetArrayElementAtIndex(i);
                    Rect row2 = EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(prop, new GUIContent($"Element {i}"));
                    if (GUILayout.Button(new GUIContent("X", "Remove Element"), GUILayout.Width(30)))
                        RemoveName(i);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUI.indentLevel--;
            //}

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add Element", GUILayout.Width(120)))
                AddElement();
            EditorGUILayout.EndHorizontal();

            //EditorGUILayout.PropertyField(userListProperty, new GUIContent("User List", "List of VRChat display names"));

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }

        void ClearList()
        {
            userListProperty.ClearArray();
        }

        void AppendNames(string text)
        {
            HashSet<string> existing = new HashSet<string>();
            for (int i = 0; i < userListProperty.arraySize; i++)
            {
                SerializedProperty prop = userListProperty.GetArrayElementAtIndex(i);
                string name = prop.stringValue;
                if (name == null || name.Length == 0)
                    continue;

                existing.Add(name);
            }

            string[] names = text.Split('\n');
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i].Trim();
                if (name.Length == 0)
                    continue;

                if (existing.Contains(name))
                    continue;

                int next = userListProperty.arraySize;
                userListProperty.InsertArrayElementAtIndex(next);
                SerializedProperty prop = userListProperty.GetArrayElementAtIndex(next);
                prop.stringValue = name;
            }
        }

        void RemoveName(int index)
        {
            if (index < 0 || index >= userListProperty.arraySize)
                return;

            userListProperty.DeleteArrayElementAtIndex(index);
        }

        void AddElement()
        {
            int next = userListProperty.arraySize;
            userListProperty.InsertArrayElementAtIndex(next);
            SerializedProperty prop = userListProperty.GetArrayElementAtIndex(next);
            prop.stringValue = "";
        }
    }
}
