
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AudioChannelGroup : UdonSharpBehaviour
    {
        [Tooltip("The name of this collection of audio channels.")]
        public string groupName;
        [Tooltip("An icon suitable for rendering in the video player UI.")]
        public Sprite groupIcon;

        [Header("Channels")]
        [Tooltip("A single channel for use with unity video sources.")]
        public AudioChannel unityChannel;
        [Tooltip("A list of channels for use with avpro video sources.")]
        public AudioChannel[] avproChannels;

        [Header("AudioLink")]
        [Tooltip("For AVPro video sources, use the reserved audio source for AudioLink instead of any of the listed channels.")]
        public bool useReservedAudioSource = false;

        void Start()
        {

        }
    }
}
