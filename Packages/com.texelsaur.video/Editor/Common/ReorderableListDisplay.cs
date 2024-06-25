using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Texel
{
    public enum HAlign
    {
        None,
        Left,
        Right,
        Center,
    };

    public abstract class ReorderableListDisplay
    {
        public float indentUnit = 15;

        protected ReorderableList list;
        protected SerializedObject serializedObject;
        protected GUIContent header;

        protected List<SerializedProperty> properties;

        static GUIContent labelDefaultAdd = new GUIContent("+");

        public ReorderableListDisplay(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            list = new ReorderableList(serializedObject, mainListProperty(serializedObject));
            list.drawElementCallback = OnDrawElement;
            list.drawHeaderCallback = OnDrawHeader;
            list.onAddCallback = OnAdd;
            list.onRemoveCallback = OnRemove;
            list.elementHeightCallback = OnElementHeight;
            list.footerHeight = -15;
            list.draggable = false;

            header = new GUIContent("List");
            properties = new List<SerializedProperty>();
        }

        protected abstract SerializedProperty mainListProperty(SerializedObject serializedObject);

        protected virtual void OnDrawHeader(Rect rect)
        {
            GUI.Label(rect, header);
        }

        protected abstract void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused);
        protected abstract float OnElementHeight(int index);

        protected virtual void OnAdd(ReorderableList list)
        {
            list.index = SerializeUtility.AddElement(properties.ToArray());

            foreach (SerializedProperty arr in properties)
            {
                SerializedProperty prop = arr.GetArrayElementAtIndex(list.index);

                string type = prop.type;
                if (type.StartsWith("PPtr"))
                    type = "ObjectReference";

                switch (type)
                {
                    case "int":
                        prop.intValue = 0;
                        break;
                    case "float":
                        prop.floatValue = 0;
                        break;
                    case "bool":
                        prop.boolValue = false;
                        break;
                    case "string":
                        prop.stringValue = "";
                        break;
                    case "Vector2":
                        prop.vector2Value = new Vector2();
                        break;
                    case "Vector3":
                        prop.vector3Value = new Vector3();
                        break;
                    case "Vector4":
                        prop.vector4Value = new Vector4();
                        break;
                    case "Vector2Int":
                        prop.vector2IntValue = new Vector2Int();
                        break;
                    case "Vector3Int":
                        prop.vector3IntValue = new Vector3Int();
                        break;
                    case "ObjectReference":
                        prop.objectReferenceValue = null;
                        break;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void OnRemove(ReorderableList list)
        {
            if (list.index < 0 || list.index >= list.count)
                return;

            SerializeUtility.RemoveElementAt(list.index, properties.ToArray());
            serializedObject.ApplyModifiedProperties();

            int count = list.serializedProperty.arraySize;
            if (list.index >= count)
                list.index = count - 1;
        }

        protected SerializedProperty AddSerializedArray(SerializedProperty prop)
        {
            properties.Add(prop);
            return prop;
        }

        public int Count
        {
            get { return list.count; }
        }

        public void Draw(int indentLevel)
        {
            Rect listRect = GUILayoutUtility.GetRect(0, list.GetHeight() + 16, GUILayout.ExpandWidth(true));
            listRect.x += indentLevel * indentUnit;
            listRect.width -= indentLevel * indentUnit;
            list.DoList(listRect);
        }

        protected Rect DrawPrefix(Rect rect, int indentLevel, GUIContent label)
        {
            Rect fieldRect = EditorGUI.PrefixLabel(rect, label);
            fieldRect.x -= indentLevel * indentUnit;

            return fieldRect;
        }

        protected bool DrawToggle(ref Rect rect, int indentLevel, GUIContent label, bool value)
        {
            Rect lineRect = Indent(ref rect, indentLevel);
            Rect fieldRect = DrawPrefix(lineRect, indentLevel, label);
            return EditorGUI.Toggle(fieldRect, value);
        }

        protected void DrawToggle(ref Rect rect, int indentLevel, GUIContent label, SerializedProperty prop)
        {
            Rect lineRect = Indent(ref rect, indentLevel);
            Rect fieldRect = DrawPrefix(lineRect, indentLevel, label);
            prop.boolValue = EditorGUI.Toggle(fieldRect, prop.boolValue);
        }

        protected Vector2Int DrawSizeField(ref Rect rect, int indentLevel, GUIContent label, Vector2Int size)
        {
            Rect lineRect = Indent(ref rect, indentLevel);
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

        protected float DrawFloatField(ref Rect rect, int indentLevel, GUIContent label, float val)
        {
            Rect lineRect = Indent(ref rect, indentLevel);
            Rect fieldRect = DrawPrefix(lineRect, indentLevel, label);
            fieldRect.width = (fieldRect.width - 18) / 2;
            return EditorGUI.DelayedFloatField(fieldRect, val);
        }

        protected void DrawFloatField(ref Rect rect, int indentLevel, GUIContent label, SerializedProperty prop)
        {
            prop.floatValue = DrawFloatField(ref rect, indentLevel, label, prop.floatValue);
        }

        protected int DrawIntField(ref Rect rect, int indentLevel, GUIContent label, int val)
        {
            Rect lineRect = Indent(ref rect, indentLevel);
            Rect fieldRect = DrawPrefix(lineRect, indentLevel, label);
            fieldRect.width = (fieldRect.width - 18) / 2;
            return EditorGUI.DelayedIntField(fieldRect, val);
        }

        protected void DrawIntField(ref Rect rect, int indentLevel, GUIContent label, SerializedProperty prop)
        {
            prop.intValue = DrawIntField(ref rect, indentLevel, label, prop.intValue);
        }

        protected T DrawEnumField<T>(ref Rect rect, int indentLevel, GUIContent label, T val) where T : Enum {
            Rect lineRect = Indent(ref rect, indentLevel);
            Rect fieldRect = DrawPrefix(lineRect, indentLevel, label);
            fieldRect.width = (fieldRect.width - 18) / 2;
            return (T)EditorGUI.EnumPopup(fieldRect, val);
        }

        protected int DrawPopupField(ref Rect rect, int indentLevel, GUIContent label, int selected, string[] options)
        {
            Rect lineRect = Indent(ref rect, indentLevel);
            Rect fieldRect = DrawPrefix(lineRect, indentLevel, label);
            //fieldRect.width = (fieldRect.width - 18) / 2;

            if (selected >= options.Length)
                selected = options.Length - 1;
            if (selected < 0)
                selected = 0;

            return EditorGUI.Popup(fieldRect, selected, options);
        }

        protected void DrawPopupField(ref Rect rect, int indentLevel, GUIContent label, SerializedProperty prop, string[] options)
        {
            prop.intValue = DrawPopupField(ref rect, indentLevel, label, prop.intValue, options);
        }

        protected void DrawPropertyField(ref Rect rect, int indentLevel, GUIContent label, SerializedProperty prop)
        {
            Rect lineRect = Indent(ref rect, indentLevel);
            Rect fieldRect = DrawPrefix(lineRect, indentLevel, label);
            EditorGUI.PropertyField(fieldRect, prop, GUIContent.none);
        }

        protected T DrawObjectField<T>(ref Rect rect, int indentLevel, GUIContent label, T obj, bool allowSceneObjects) where T : UnityEngine.Object
        {
            Rect lineRect = Indent(ref rect, indentLevel);
            Rect fieldRect = DrawPrefix(lineRect, indentLevel, label);
            return (T)EditorGUI.ObjectField(fieldRect, obj, typeof(T), allowSceneObjects);
        }

        protected void DrawObjectField(ref Rect rect, int indentLevel, GUIContent label, SerializedProperty prop)
        {
            Rect lineRect = Indent(ref rect, indentLevel);
            Rect fieldRect = DrawPrefix(lineRect, indentLevel, label);
            EditorGUI.ObjectField(fieldRect, prop, GUIContent.none);
        }

        protected bool DrawObjectFieldWithAdd(ref Rect rect, int indentLevel, GUIContent label, SerializedProperty prop)
        {
            return DrawObjectFieldWithAdd(ref rect, indentLevel, label, prop, labelDefaultAdd, EditorGUIUtility.singleLineHeight, GUI.backgroundColor);
        }

        protected bool DrawObjectFieldWithAdd(ref Rect rect, int indentLevel, GUIContent label, SerializedProperty prop, GUIContent addLabel, float addWidth)
        {
            return DrawObjectFieldWithAdd(ref rect, indentLevel, label, prop, addLabel, addWidth, GUI.backgroundColor);
        }

        protected bool DrawObjectFieldWithAdd(ref Rect rect, int indentLevel, GUIContent label, SerializedProperty prop, GUIContent addLabel, float addWidth, Color buttonColor)
        {
            Rect lineRect = Indent(ref rect, indentLevel);
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

        protected bool DrawButton(ref Rect rect, int indentLevel, GUIContent label, HAlign align, float width)
        {
            Rect lineRect = Indent(ref rect, indentLevel);

            switch (align)
            {
                case HAlign.Right:
                    lineRect.x += (lineRect.width - width);
                    break;
                case HAlign.Center:
                    lineRect.x += (lineRect.width - width) / 2;
                    break;
            }
            lineRect.width = width;

            return GUI.Button(lineRect, label);
        }

        protected Rect Indent(ref Rect rect, int indentLevel)
        {
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            Rect indented = rect;
            indented.x += indentLevel * indentUnit;
            return indented;
        }

        protected void InitRect(ref Rect rect)
        {
            rect.height = EditorGUIUtility.singleLineHeight;
            rect.y -= EditorGUIUtility.singleLineHeight;
        }
    }
}
