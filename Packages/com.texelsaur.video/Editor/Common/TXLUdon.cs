using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.Udon;

namespace Texel
{
    public class TXLUdon
    {
        public static UdonBehaviour FindExternal(UdonBehaviour cache, string scriptName)
        {
            if (cache)
            {
                UdonBehaviour[] components = cache.transform.GetComponents<UdonBehaviour>();
                cache = FindBehaviour(components, scriptName);
                if (cache)
                    return cache;
            }

            UdonBehaviour[] allBehaviours = Editor.FindObjectsOfType<UdonBehaviour>();
            cache = FindBehaviour(allBehaviours, scriptName);
            return cache;
        }

        public static UdonBehaviour FindBehaviour(UdonBehaviour[] behaviors, string scriptName)
        {
            foreach (UdonBehaviour behaviour in behaviors)
            {
                if (!behaviour.programSource)
                    continue;

                if (behaviour.programSource.name != scriptName)
                    continue;

                return behaviour;
            }

            return null;
        }

        public static void LinkProperty(SerializedProperty property, UdonBehaviour component)
        {
            if (component)
                property.objectReferenceValue = component;
            else
                property.objectReferenceValue = null;
        }
    }
}
