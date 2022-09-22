
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlaylistCatalog : UdonSharpBehaviour
    {
        public string catalogName;
        public PlaylistData[] playlists;

        public int PlaylistCount
        {
            get
            {
                if (!Utilities.IsValid(playlists))
                    return 0;
                return playlists.Length;
            }
        }
    }
}
