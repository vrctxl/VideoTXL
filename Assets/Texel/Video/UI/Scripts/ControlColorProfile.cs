
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
        public Color backgroundColor = new Color(0.10f, 0.15f, 0.12f);
        public Color backgroundMsgBarColor = new Color(0.05f, 0.08f, 0.06f);
        public Color buttonColor = new Color(0.14f, 0.19f, 0.16f);
        public Color buttonSelectedColor = new Color(.33f, .43f, .37f);
        public Color brightLabelColor = new Color(.9f, .9f, .9f);
        public Color dimLabelColor = new Color(.5f, .5f, .5f);
        public Color redLabelColor = new Color(.8f, 0f, 0f);
        public Color brightSliderColor = new Color(.27f, .40f, .37f);
        public Color dimSliderColor = new Color(.2f, .27f, .26f);
        public Color sliderGrabColor = new Color(.4f, .58f, .52f);
    }
}
