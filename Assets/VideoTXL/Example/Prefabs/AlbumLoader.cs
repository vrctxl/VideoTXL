
using UdonSharp;
using UnityEngine;
using VideoTXL;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [AddComponentMenu("Texel/Album/Album Loader")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class AlbumLoader : UdonSharpBehaviour
    {
        public SyncPlayer player;
        public Transform displayTransform;
        public int maxAlbumCount = 500;

        public AudioSource loadSound;
        public AudioSource errorSound;

        Album[] albums;

        [UdonSynced]
        int syncLoadedIndex = -1;

        void Start()
        {
            _Init();
        }

        void _Init()
        {
            if (Utilities.IsValid(albums))
                return;

            albums = new Album[maxAlbumCount];
        }

        public void _Register(Album album)
        {
            _Init();
            if (!Utilities.IsValid(album))
                return;

            albums[album.albumId] = album;
        }

        public void _LoadAlbum(Album album)
        {
            if (!Utilities.IsValid(album) || !player._CanTakeControl())
            {
                if (Utilities.IsValid(errorSound))
                    errorSound.Play();
                return;
            }

            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            if (syncLoadedIndex >= 0)
            {
                Album loadedAlbum = albums[syncLoadedIndex];
                if (album.albumId != syncLoadedIndex && Utilities.IsValid(loadedAlbum))
                    loadedAlbum._Reset();
            }

            if (Utilities.IsValid(loadSound))
                loadSound.Play();

            player._ChangeUrl(album.url);

            syncLoadedIndex = album.albumId;
            RequestSerialization();

            album._Display(displayTransform);
        }

        public void _PickupAlbum(Album album)
        {
            if (syncLoadedIndex >= 0 && album.albumId == syncLoadedIndex)
            {
                if (!Networking.IsOwner(gameObject))
                    Networking.SetOwner(Networking.LocalPlayer, gameObject);

                syncLoadedIndex = -1;
                RequestSerialization();
            }
        }
    }
}
