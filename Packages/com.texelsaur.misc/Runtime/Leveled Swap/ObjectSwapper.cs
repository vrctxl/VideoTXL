using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Texel
{
    [AddComponentMenu("Texel/State/Object Swapper")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ObjectSwapper : UdonSharpBehaviour
    {
        public GameObject[] objectList;
        public GameObject[] replacementList;

        public void _Reset()
        {
            for (int i = 0; i < objectList.Length; i++)
            {
                if (Utilities.IsValid(objectList[i]))
                    objectList[i].SetActive(true);

                if (Utilities.IsValid(replacementList[i]))
                    replacementList[i].SetActive(false);
            }
        }

        public void _Apply()
        {
            for (int i = 0; i < objectList.Length; i++)
            {
                if (Utilities.IsValid(objectList[i]))
                    objectList[i].SetActive(false);

                if (Utilities.IsValid(replacementList[i]))
                    replacementList[i].SetActive(true);
            }
        }
    }
}
