using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace Texel
{
    public class ControlUtils
    {
        public static void CollectObjects<T>(List<Object> list, GameObject root, string[] paths) where T : Object
        {
            foreach (string path in paths)
            {
                Transform t = root.transform.Find(path);
                if (t == null)
                    continue;

                T component = t.GetComponent<T>();
                if (component == null)
                    continue;

                list.Add(component);
            }
        }

        public static void UpdateImages(GameObject root, string[] paths, Color color)
        {
            foreach (string path in paths)
            {
                Transform t = root.transform.Find(path);
                if (t == null)
                    continue;

                Image image = t.GetComponent<Image>();
                if (image == null)
                    continue;

                image.color = color;
            }
        }

        public static void UpdateTexts(GameObject root, string[] paths, Color color)
        {
            foreach (string path in paths)
            {
                Transform t = root.transform.Find(path);
                if (t == null)
                    continue;

                Text text = t.GetComponent<Text>();
                if (text == null)
                    continue;

                text.color = color;
            }
        }

        public static GameObject FindGameObject(GameObject root, string path)
        {
            while (path.StartsWith(".."))
            {
                if (root.transform.parent == null)
                    return null;

                root = root.transform.parent.gameObject;
                path = Regex.Replace(path, "^../?", "");
            }

            Transform t = root.transform.Find(path);
            if (t == null)
                return null;

            return t.gameObject;
        }

        public static Component FindComponent(GameObject root, string path, System.Type type)
        {
            GameObject obj = FindGameObject(root, path);
            if (obj == null)
                return null;

            return obj.transform.GetComponent(type);
        }
    }
}