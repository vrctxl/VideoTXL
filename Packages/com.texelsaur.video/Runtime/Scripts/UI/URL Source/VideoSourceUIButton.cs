
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VideoSourceUIButton : UdonSharpBehaviour
    {
        [SerializeField] internal GameObject activeText;
        [SerializeField] internal GameObject inactiveText;

        [HideInInspector] public VideoSourceUI sourceUI;
        [HideInInspector] public int sourceIndex = -1;

        public void _Init(VideoSourceUI sourceUI, int sourceIndex, string name)
        {
            this.sourceUI = sourceUI;
            this.sourceIndex = sourceIndex;

            Text[] text = GetComponentsInChildren<Text>();
            foreach (var t in text)
                t.text = name;

            _SetActive(false);
        }

        public void _Select()
        {
            if (sourceUI)
                sourceUI._Select(sourceIndex);
        }

        public void _SetActive(bool state)
        {
            if (activeText)
                activeText.SetActive(state);
            if (inactiveText)
                inactiveText.SetActive(!state);
        }
    }
}
