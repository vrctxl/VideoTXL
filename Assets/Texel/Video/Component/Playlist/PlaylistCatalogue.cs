
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class PlaylistCatalogue : UdonSharpBehaviour
    {
        public string catalogueName;
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
