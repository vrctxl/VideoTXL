
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [AddComponentMenu("VideoTXL/UI/Control Color Profile")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ControlColorProfile : UdonSharpBehaviour
    {
        [Header("State Colors")]
        public Color normalColor = new Color(1f, 1f, 1f, .8f);
        public Color disabledColor = new Color(.5f, .5f, .5f, .4f);
        public Color activeColor = new Color(0f, 1f, .5f, .7f);
        public Color attentionColor = new Color(.9f, 0f, 0f, .5f);

        [Header("Background Colors")]
        public Color backgroundColor = new Color(0.09f, 0.15f, 0.11f);
        public Color backgroundTitleColor = new Color(0.05f, 0.09f, 0.07f);
        public Color buttonBackgroundColor = new Color(0.14f, 0.19f, 0.16f);
        public Color sliderBackgroundColor = new Color(0.19f, 0.25f, 0.23f);
        public Color volumeFillColor = new Color(0.22f, 0.32f, 0.29f);
        public Color volumeHandleColor = new Color(0.33f, 0.48f, 0.43f);
        public Color trackerFillColor = new Color(0.27f, 0.40f, 0.37f);
        public Color trackerHandleColor = new Color(0.38f, 0.55f, 0.50f);

        [Header("Text Colors")]
        public Color generalTextColor = new Color(0.85f, 0.85f, 0.85f, 1.0f);
        public Color mainTextColor = new Color(0.8f, 0.8f, 0.8f, 1.0f);
        public Color subTextColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);
    }
}
