
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlaylistLoadData : UdonSharpBehaviour
    {
        public Playlist playlist;
        public PlaylistData playlistData;

        public void _Load()
        {
            if (playlist)
            {
                if (playlist.playlistCatalog)
                    playlist._LoadFromCatalogueData(playlistData);
                else
                    playlist._LoadData(playlistData);
            }
        }

        public void _LoadFromCatalog()
        {
            if (playlist)
                playlist._LoadFromCatalogueData(playlistData);
        }

        public void _LoadLocal()
        {
            if (playlist)
                playlist._LoadData(playlistData);
        }
    }
}
