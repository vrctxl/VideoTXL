using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Texel
{
    public class TXLEditor
    {
        public static bool DrawFoldoutHeader(GUIContent label, bool expanded)
        {
            expanded = EditorGUILayout.Foldout(expanded, label);
            EditorGUILayout.Space();

            return expanded;
        }

        public static bool DrawFoldoutHeader(GUIContent label, ref bool expanded)
        {
            expanded = DrawFoldoutHeader(label, expanded);
            return expanded;
        }

        public static bool DrawMainHeader(GUIContent label, bool expanded)
        {
            expanded = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, label);
            EditorGUILayout.Space();
            EditorGUILayout.EndFoldoutHeaderGroup();

            return expanded;
        }

        public static bool DrawMainHeaderHelp(GUIContent label, bool expanded, string helpUrl)
        {
            Texture2D icon = EditorGUIUtility.FindTexture("_Help");
            GUIStyle iconStyle = new GUIStyle(EditorStyles.foldoutHeaderIcon);
            iconStyle.normal.background = icon;

            Action<Rect> openUrlAction = r => {
                Application.OpenURL(helpUrl);
            };

            EditorGUILayout.BeginHorizontal();
            expanded = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, label, null, openUrlAction, iconStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            return expanded;
        }

        public static bool DrawMainHeaderHelp(GUIContent label, ref bool expanded, string helpUrl)
        {
            expanded = DrawMainHeaderHelp(label, expanded, helpUrl);
            return expanded;
        }

        public static void IndentedHelpBox(int level, string message, MessageType messageType)
        {
            EditorGUI.indentLevel += level;
            EditorGUILayout.HelpBox(message, messageType);
            EditorGUI.indentLevel -= level;
        }

        public static void IndentedHelpBox(string message, MessageType messageType)
        {
            IndentedHelpBox(1, message, messageType);
        }
    }
}
