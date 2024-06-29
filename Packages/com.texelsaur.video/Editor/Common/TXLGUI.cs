using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Texel
{
    public class TXLGUI
    {
        public static float indentUnit = 15;

        protected static Rect DrawPrefix(Rect rect, int indentLevel, GUIContent label)
        {
            Rect fieldRect = EditorGUI.PrefixLabel(rect, label);
            fieldRect.x -= indentLevel * indentUnit;

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
    }
}
