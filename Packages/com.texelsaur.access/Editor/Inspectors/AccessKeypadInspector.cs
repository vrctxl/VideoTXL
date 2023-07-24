using System.Collections;
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace Texel
{
    [CustomEditor(typeof(AccessKeypad))]
    public class AccessKeypadInspector : Editor
    {
        static bool[] _showWhitelistFoldout = new bool[0];
        static bool[] _showFunctionFoldout = new bool[0];

        SerializedProperty keypadsProperty;
        SerializedProperty whitelistCodesProperty;
        SerializedProperty dynamicListsProperty;
        SerializedProperty functionCodesProperty;
        SerializedProperty functionTargetsProperty;
        SerializedProperty functionNamesProperty;
        SerializedProperty functionArgsProperty;

        private void OnEnable()
        {
            keypadsProperty = serializedObject.FindProperty("keypads");
            whitelistCodesProperty = serializedObject.FindProperty("whitelistCodes");
            dynamicListsProperty = serializedObject.FindProperty("dynamicLists");
            functionCodesProperty = serializedObject.FindProperty("functionCodes");
            functionTargetsProperty = serializedObject.FindProperty("functionTargets");
            functionNamesProperty = serializedObject.FindProperty("functionNames");
            functionArgsProperty = serializedObject.FindProperty("functionArgs");
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(keypadsProperty);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Managing dynamic whitelists via access code is less secure than other methods like Whitelist Grant.", MessageType.Warning);
            EditorGUILayout.LabelField("Dynamic Whitelists", EditorStyles.boldLabel);
            WhitelistFoldout();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Functions will only be invoked for the local player when the correct code is entered.", MessageType.Info);
            EditorGUILayout.LabelField("Function Calls", EditorStyles.boldLabel);
            FunctionFoldout();

            serializedObject.ApplyModifiedProperties();
        }

        private void WhitelistFoldout()
        {
            _showWhitelistFoldout = EditorTools.MultiArraySize(serializedObject, _showWhitelistFoldout,
                whitelistCodesProperty, dynamicListsProperty);

            for (int i = 0; i < whitelistCodesProperty.arraySize; i++)
            {
                string name = EditorTools.GetObjectName(dynamicListsProperty, i);
                _showWhitelistFoldout[i] = EditorGUILayout.Foldout(_showWhitelistFoldout[i], $"Whitelist {i} ({name})");
                if (_showWhitelistFoldout[i])
                {
                    EditorGUI.indentLevel++;

                    SerializedProperty code = whitelistCodesProperty.GetArrayElementAtIndex(i);
                    SerializedProperty whitelist = dynamicListsProperty.GetArrayElementAtIndex(i);

                    EditorGUILayout.PropertyField(code, new GUIContent("Code"));
                    EditorGUILayout.PropertyField(whitelist, new GUIContent("Dynamic Whitelist"));

                    AccessControlDynamicUserList dynlist = (AccessControlDynamicUserList)whitelist.objectReferenceValue;
                    if (dynlist)
                    {
                        if (!dynlist.syncedPlayerList)
                            EditorGUILayout.HelpBox("The referenced dynamic user list doesn't have a backing SyncPlayerList set.", MessageType.Error);
                        else if (!dynlist.syncedPlayerList.allowOwnershipTransfer)
                            EditorGUILayout.HelpBox("The referenced dynamic user list doesn't have Allow Ownership Transfer set, a player will not be able to add their name to the synced list.", MessageType.Error);
                        else if (dynlist.syncedPlayerList.accessControl)
                            EditorGUILayout.HelpBox("The referenced dynamic user list has an Access Control object set.  Only players who already have permission through that ACL will be able to add their name to the target synced list.", MessageType.Warning);
                    }

                    EditorGUI.indentLevel--;
                }
            }
        }

        private void FunctionFoldout()
        {
            _showFunctionFoldout = EditorTools.MultiArraySize(serializedObject, _showFunctionFoldout,
                functionCodesProperty, functionTargetsProperty, functionNamesProperty, functionArgsProperty);

            for (int i = 0; i < functionCodesProperty.arraySize; i++)
            {
                string name = EditorTools.GetObjectName(functionTargetsProperty, i);
                _showFunctionFoldout[i] = EditorGUILayout.Foldout(_showFunctionFoldout[i], $"Function {i} ({name})");
                if (_showFunctionFoldout[i])
                {
                    EditorGUI.indentLevel++;

                    SerializedProperty code = functionCodesProperty.GetArrayElementAtIndex(i);
                    SerializedProperty funcTarget = functionTargetsProperty.GetArrayElementAtIndex(i);
                    SerializedProperty funcName = functionNamesProperty.GetArrayElementAtIndex(i);
                    SerializedProperty funcArg = functionArgsProperty.GetArrayElementAtIndex(i);

                    EditorGUILayout.PropertyField(code, new GUIContent("Code"));
                    EditorGUILayout.PropertyField(funcTarget, new GUIContent("Target Script"));
                    EditorGUILayout.PropertyField(funcName, new GUIContent("Function Name"));
                    EditorGUILayout.PropertyField(funcArg, new GUIContent("Player Field Name", "Optional.  Name of field on target script where a reference to the player entering the code can be set."));

                    EditorGUI.indentLevel--;
                }
            }
        }
    }
}
