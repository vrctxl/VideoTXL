
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VideoSourceAudioGroup : UdonSharpBehaviour
    {
        public string groupName;

        [Header("Channel Data")]
        public string[] channelName;
        public AudioSource[] channelAudio;
        public AudioChannel[] channelReference;

        //public float[] channelVolume;
        //public bool[] channelMute;
        //public AudioFadeZone[] channelFadeZone;

        void Start()
        {

        }
    }
}
