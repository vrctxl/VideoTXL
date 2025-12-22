using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Texel
{
    public class TXLGUI
    {
        public static float indentUnit = 15;
        public static GUIContent labelDefaultAdd = new GUIContent("+");

        protected static Rect DrawPrefix(Rect rect, int indentLevel, GUIContent label)
        {
            Rect fieldRect = EditorGUI.PrefixLabel(rect, label);
            fieldRect.x -= (indentLevel + EditorGUI.indentLevel) * indentUnit;

            return fieldRect;
        }

        protected static Rect Indent(Rect rect, int indentLevel)
        {
            Rect indented = rect;
            indented.x += indentLevel * indentUnit;
            return indented;
        }

        public static Vector2Int DrawSizeField(Rect rect, int indentLevel, GUIContent label, Vector2Int size)
        {
            Rect lineRect = Indent(rect, indentLevel);
            Rect fieldRect = DrawPrefix(lineRect, indentLevel, label);
            Rect field1 = fieldRect;
            float xwidth = 8;
            float xpad = 5;
            float fwidth = (fieldRect.width - xwidth - xpad * 2) / 2;

            field1.width = fwidth;
            int width = EditorGUI.DelayedIntField(field1, size.x);
            field1.x += fwidth + xpad;
            field1.width = xwidth;
            EditorGUI.LabelField(field1, "x");

            field1.x += xwidth + xpad;
            field1.width = fwidth;
            int height = EditorGUI.DelayedIntField(field1, size.y);

            return new Vector2Int(width, height);
        }

        public static void DrawToggle2(Rect rect, int indentLevel, GUIContent label, GUIContent label1, SerializedProperty prop1, GUIContent label2, SerializedProperty prop2)
        {
            Rect lineRect = Indent(rect, indentLevel);
            Rect fieldRect = DrawPrefix(lineRect, indentLevel, label);

            Rect field1 = fieldRect;
            field1.width /= 2;
            Rect field2 = field1;
            field2.x = field1.xMax;

            prop1.boolValue = EditorGUI.Toggle(field1, prop1.boolValue);
            field1.x += 20;
            field1.width -= 20;
            EditorGUI.LabelField(field1, label1);

            prop2.boolValue = EditorGUI.Toggle(field2, prop2.boolValue);
            field2.x += 20;
            field2.width -= 20;
            EditorGUI.LabelField(field2, label2);
        }
        public static bool DrawObjectFieldWithAdd(SerializedProperty property, GUIContent label)
        {
            return DrawObjectFieldWithAdd(EditorGUILayout.GetControlRect(), EditorGUI.indentLevel, label, property, labelDefaultAdd);
        }

        public static bool DrawObjectFieldWithAdd(SerializedProperty property, GUIContent label, GUIContent addLabel)
        {
            return DrawObjectFieldWithAdd(EditorGUILayout.GetControlRect(), EditorGUI.indentLevel, label, property, addLabel);
        }

        public static bool DrawObjectFieldWithAdd(Rect rect, int indentLevel, GUIContent label, SerializedProperty prop)
        {
            return DrawObjectFieldWithAdd(rect, indentLevel, label, prop, labelDefaultAdd, EditorGUIUtility.singleLineHeight, GUI.backgroundColor);
        }

        public static bool DrawObjectFieldWithAdd(Rect rect, int indentLevel, GUIContent label, SerializedProperty prop, GUIContent addLabel)
        {
            return DrawObjectFieldWithAdd(rect, indentLevel, label, prop, addLabel, EditorGUIUtility.singleLineHeight, GUI.backgroundColor);
        }

        public static bool DrawObjectFieldWithAdd(Rect rect, int indentLevel, GUIContent label, SerializedProperty prop, GUIContent addLabel, float addWidth)
        {
            return DrawObjectFieldWithAdd(rect, indentLevel, label, prop, addLabel, addWidth, GUI.backgroundColor);
        }

        public static bool DrawObjectFieldWithAdd(Rect rect, int indentLevel, GUIContent label, SerializedProperty prop, GUIContent addLabel, float addWidth, Color buttonColor)
        {
            Rect lineRect = Indent(rect, indentLevel);
            Rect fieldRect = DrawPrefix(lineRect, indentLevel, label);
            Rect buttonRect = new Rect(fieldRect.x + fieldRect.width - addWidth, fieldRect.y, addWidth, fieldRect.height);
            fieldRect.width -= buttonRect.width + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.ObjectField(fieldRect, prop, GUIContent.none);

            Color bgColor = GUI.backgroundColor;
            GUI.backgroundColor = buttonColor;
            bool result = GUI.Button(buttonRect, addLabel, EditorStyles.miniButton);
            GUI.backgroundColor = bgColor;

            return result;
        }
    }
}
