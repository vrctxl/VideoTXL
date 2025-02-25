
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VideoSourceUIButton : UdonSharpBehaviour
    {
        [HideInInspector] public VideoSourceUI sourceUI;
        [HideInInspector] public int sourceIndex = -1;

        public void _Init(VideoSourceUI sourceUI, int sourceIndex, string name)
        {
            this.sourceUI = sourceUI;
            this.sourceIndex = sourceIndex;

            Text text = GetComponentInChildren<Text>();
            if (text)
                text.text = name;
        }

        public void _Select()
        {
            if (sourceUI)
                sourceUI._Select(sourceIndex);
        }
    }
}
