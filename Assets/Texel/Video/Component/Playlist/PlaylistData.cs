
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class PlaylistData : UdonSharpBehaviour
    {
        public string playlistName;

        public int questFallbackType = FALLBACK_NONE;
        [Tooltip("Custom prefix to apply to all URLs when loading on a Quest device")]
        public string questCustomPrefix;

        public VRCUrl[] playlist;
        public VRCUrl[] questPlaylist;
        public string[] trackNames;

        public const int FALLBACK_NONE = 0;
        public const int FALLBACK_JINNAI = 1;
        public const int FALLBACK_CUSTOM = 2;
        public const int FALLBACK_INDIVIDUAL = 3;

        private void Start()
        {

        }
    }
}
