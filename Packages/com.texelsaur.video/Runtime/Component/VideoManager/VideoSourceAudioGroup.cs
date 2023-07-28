
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
        public AudioChannel[] channelReference;
        public AudioSource[] channelAudio;

        void Start()
        {

        }
    }
}
