using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.Udon;

namespace Texel
{
    [InitializeOnLoad]
    public class VideoTxlManager
    {
        [MenuItem("Tools/TXL/VideoTXL/Add \"Sync Video Player\" Prefab to Scene", false)]
        [MenuItem("GameObject/TXL/VideoTXL/Sync Video Player", false, 100)]
        public static void AddSyncPlayerToScene()
        {
            Selection.activeObject = MenuUtil.AddPrefabToActiveOrScene("Packages/com.texelsaur.video/Runtime/Prefabs/Sync Video Player.prefab");
        }

        [MenuItem("GameObject/TXL/VideoTXL/Other Video Players/Sync Video Player Template", false, 201)]
        public static void AddSyncPlayerTemplateToScene()
        {
            Selection.activeObject = MenuUtil.AddPrefabToActiveOrScene("Packages/com.texelsaur.video/Runtime/Prefabs/Other Video Players/Base/Sync Video Player Base.prefab");
        }

        [MenuItem("GameObject/TXL/VideoTXL/Other Video Players/Local Video Player Template", false, 202)]
        public static void AddLocalPlayerTemplateToScene()
        {
            Selection.activeObject = MenuUtil.AddPrefabToActiveOrScene("Packages/com.texelsaur.video/Runtime/Prefabs/Other Video Players/Base/Local Video Player Base.prefab");
        }

        [MenuItem("GameObject/TXL/VideoTXL/Other Video Players/Basic Sync Video Player", false, 301)]
        public static void AddBasicSyncPlayerToScene()
        {
            Selection.activeObject = MenuUtil.AddPrefabToActiveOrScene("Packages/com.texelsaur.video/Runtime/Prefabs/Other Video Players/Basic Sync Video Player.prefab");
        }

        [MenuItem("GameObject/TXL/VideoTXL/Other Video Players/Local Video Player AVPro", false, 302)]
        public static void AddLocalAVProPlayerToScene()
        {
            Selection.activeObject = MenuUtil.AddPrefabToActiveOrScene("Packages/com.texelsaur.video/Runtime/Prefabs/Other Video Players/Local Video Player AVPro.prefab");
        }

        [MenuItem("GameObject/TXL/VideoTXL/Other Video Players/Local Video Player Unity", false, 303)]
        public static void AddLocalUnityPlayerToScene()
        {
            Selection.activeObject = MenuUtil.AddPrefabToActiveOrScene("Packages/com.texelsaur.video/Runtime/Prefabs/Other Video Players/Local Video Player Unity.prefab");
        }

        [MenuItem("GameObject/TXL/VideoTXL/Other Video Players/Sync Video Player Full", false, 304)]
        public static void AddSyncPlayerFullToScene()
        {
            Selection.activeObject = MenuUtil.AddPrefabToActiveOrScene("Packages/com.texelsaur.video/Runtime/Prefabs/Other Video Players/Sync Video Player Full.prefab");
        }

        [MenuItem("GameObject/TXL/VideoTXL/UI/Sync Video Player Controls", false, 211)]
        public static void AddSyncPlayerControlsToScene()
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.texelsaur.video/Runtime/Prefabs/UI/PlayerControls.prefab");
            if (asset != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);

                if (Selection.activeTransform) {
                    SyncPlayer vp = Selection.activeTransform.GetComponent<SyncPlayer>();
                    if (!vp)
                        vp = Selection.activeTransform.GetComponentInParent<SyncPlayer>();

                    if (vp)
                    {
                        PlayerControls com = instance.GetComponent<PlayerControls>();
                        if (com)
                            com.videoPlayer = vp;

                        AudioManager audioMan = vp.GetComponentInChildren<AudioManager>();
                        if (audioMan && audioMan.videoPlayer == vp)
                            com.audioManager = audioMan;

                        instance.transform.SetParent(vp.transform);
                    }
                }

                EditorGUIUtility.PingObject(instance);
            }
        }

        [MenuItem("GameObject/TXL/VideoTXL/UI/Local Video Player Controls", false, 212)]
        public static void AddLocalPlayerControlsToScene()
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.texelsaur.video/Runtime/Prefabs/UI/LocalControlsSlim.prefab");
            if (asset != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);

                if (Selection.activeTransform)
                {
                    TXLVideoPlayer vp = Selection.activeTransform.GetComponent<TXLVideoPlayer>();
                    if (!vp)
                        vp = Selection.activeTransform.GetComponentInParent<TXLVideoPlayer>();

                    if (vp)
                    {
                        LocalControlsSlim com = instance.GetComponent<LocalControlsSlim>();
                        if (com)
                            com.videoPlayer = vp;

                        AudioManager audioMan = vp.GetComponentInChildren<AudioManager>();
                        if (audioMan && audioMan.videoPlayer == vp)
                            com.audioManager = audioMan;

                        instance.transform.SetParent(vp.transform);
                    }
                }

                EditorGUIUtility.PingObject(instance);
            }
        }

        [MenuItem("GameObject/TXL/VideoTXL/UI/Playlist UI", false, 300)]
        public static void AddPlaylistUIToScene()
        {
            TXLVideoPlayer vp = GetVideoPlayer();
            if (vp)
            {
                GameObject playlistObj = MenuUtil.AddPrefabToObject("Packages/com.texelsaur.video/Runtime/Prefabs/UI/PlaylistUI.prefab", vp.transform);
                if (vp is SyncPlayer && ((SyncPlayer)vp).urlSource is Playlist)
                {
                    PlaylistUI ui = playlistObj.GetComponent<PlaylistUI>();
                    if (ui)
                        ui.playlist = ((SyncPlayer)vp).urlSource as Playlist;
                }
            }
            else
                MenuUtil.AddPrefabToScene("Packages/com.texelsaur.video/Runtime/Prefabs/UI/PlaylistUI.prefab");
        }

        [MenuItem("GameObject/TXL/VideoTXL/UI/Load Playlist Button", false, 301)]
        public static void AddPlaylistLoadButtonToScene()
        {
            TXLVideoPlayer vp = GetVideoPlayer();
            if (vp)
            {
                GameObject playlistObj = MenuUtil.AddPrefabToObject("Packages/com.texelsaur.video/Runtime/Prefabs/UI/PlaylistLoadButton.prefab", vp.transform);
                if (vp is SyncPlayer && ((SyncPlayer)vp).urlSource is Playlist)
                {
                    PlaylistLoadData ui = playlistObj.GetComponent<PlaylistLoadData>();
                    if (ui)
                        ui.playlist = ((SyncPlayer)vp).urlSource as Playlist;
                }
            }
            else
                MenuUtil.AddPrefabToScene("Packages/com.texelsaur.video/Runtime/Prefabs/UI/PlaylistLoadButton.prefab");
        }

        [MenuItem("GameObject/TXL/VideoTXL/Video Sources/Add Unity Video Source", true, 220)]
        public static bool AddUnityVideoSourceTest()
        {
            return GetVideoManagerRoot() != null;
        }

        [MenuItem("GameObject/TXL/VideoTXL/Video Sources/Add Unity Video Source", false, 220)]
        public static void AddUnityVideoSource()
        {
            AddVideoSource("Packages/com.texelsaur.video/Runtime/Prefabs/Video Sources/UnityTemplate.prefab", GetVideoManagerRoot());
        }

        [MenuItem("GameObject/TXL/VideoTXL/Video Sources/Add AVPro Source", true, 221)]
        public static bool AddAVProSourceTest()
        {
            return GetVideoManagerRoot() != null;
        }

        [MenuItem("GameObject/TXL/VideoTXL/Video Sources/Add AVPro Source", false, 221)]
        public static void AddAVProSource()
        {
            AddVideoSource("Packages/com.texelsaur.video/Runtime/Prefabs/Video Sources/AVProTemplate.prefab", GetVideoManagerRoot());
        }

        [MenuItem("GameObject/TXL/VideoTXL/Video Sources/Add Unity Video 1080p Source", true, 300)]
        public static bool AddUnityVideoSource1080Test()
        {
            return GetVideoManagerRoot() != null;
        }

        [MenuItem("GameObject/TXL/VideoTXL/Video Sources/Add Unity Video 1080p Source", false, 300)]
        public static void AddUnityVideoSource1080()
        {
            AddVideoSource("Packages/com.texelsaur.video/Runtime/Prefabs/Video Sources/Unity1080.prefab", GetVideoManagerRoot());
        }

        [MenuItem("GameObject/TXL/VideoTXL/Video Sources/Add Unity Video 720p Source", true, 301)]
        public static bool AddUnityVideoSource720Test()
        {
            return GetVideoManagerRoot() != null;
        }

        [MenuItem("GameObject/TXL/VideoTXL/Video Sources/Add Unity Video 720p Source", false, 301)]
        public static void AddUnityVideoSource720()
        {
            AddVideoSource("Packages/com.texelsaur.video/Runtime/Prefabs/Video Sources/Unity720.prefab", GetVideoManagerRoot());
        }

        [MenuItem("GameObject/TXL/VideoTXL/Video Sources/Add Unity Video 360p Source", true, 302)]
        public static bool AddUnityVideoSource360Test()
        {
            return GetVideoManagerRoot() != null;
        }

        [MenuItem("GameObject/TXL/VideoTXL/Video Sources/Add Unity Video 360p Source", false, 302)]
        public static void AddUnityVideoSource360()
        {
            AddVideoSource("Packages/com.texelsaur.video/Runtime/Prefabs/Video Sources/Unity360.prefab", GetVideoManagerRoot());
        }

        [MenuItem("GameObject/TXL/VideoTXL/Video Sources/Add AVPro 1080p Source", true, 400)]
        public static bool AddAVProSource1080Test()
        {
            return GetVideoManagerRoot() != null;
        }

        [MenuItem("GameObject/TXL/VideoTXL/Video Sources/Add AVPro 1080p Source", false, 400)]
        public static void AddAVProSource1080()
        {
            AddVideoSource("Packages/com.texelsaur.video/Runtime/Prefabs/Video Sources/AVPro1080.prefab", GetVideoManagerRoot());
        }

        [MenuItem("GameObject/TXL/VideoTXL/Video Sources/Add AVPro 1080p Low-Latency Source", true, 401)]
        public static bool AddAVProSource1080LLTest()
        {
            return GetVideoManagerRoot() != null;
        }

        [MenuItem("GameObject/TXL/VideoTXL/Video Sources/Add AVPro 1080p Low-Latency Source", false, 401)]
        public static void AddAVProSource1080LL()
        {
            AddVideoSource("Packages/com.texelsaur.video/Runtime/Prefabs/Video Sources/AVPro1080LL.prefab", GetVideoManagerRoot());
        }

        [MenuItem("GameObject/TXL/VideoTXL/Video Sources/Add AVPro 720p Source", true, 402)]
        public static bool AddAVProSource720Test()
        {
            return GetVideoManagerRoot() != null;
        }

        [MenuItem("GameObject/TXL/VideoTXL/Video Sources/Add AVPro 720p Source", false, 402)]
        public static void AddAVProSource720()
        {
            AddVideoSource("Packages/com.texelsaur.video/Runtime/Prefabs/Video Sources/AVPro720.prefab", GetVideoManagerRoot());
        }

        [MenuItem("GameObject/TXL/VideoTXL/Video Sources/Add AVPro 720p Low-Latency Source", true, 403)]
        public static bool AddAVProSource720LLTest()
        {
            return GetVideoManagerRoot() != null;
        }

        [MenuItem("GameObject/TXL/VideoTXL/Video Sources/Add AVPro 720p Low-Latency Source", false, 403)]
        public static void AddAVProSource720LL()
        {
            AddVideoSource("Packages/com.texelsaur.video/Runtime/Prefabs/Video Sources/AVPro720LL.prefab", GetVideoManagerRoot());
        }

        [MenuItem("GameObject/TXL/VideoTXL/Video Sources/Add AVPro 360p Source", true, 404)]
        public static bool AddAVProSource360Test()
        {
            return GetVideoManagerRoot() != null;
        }

        [MenuItem("GameObject/TXL/VideoTXL/Video Sources/Add AVPro 360p Source", false, 404)]
        public static void AddAVProSource360()
        {
            AddVideoSource("Packages/com.texelsaur.video/Runtime/Prefabs/Video Sources/AVPro360.prefab", GetVideoManagerRoot());
        }

        [MenuItem("GameObject/TXL/VideoTXL/Video Sources/Add AVPro 360p Low-Latency Source", true, 405)]
        public static bool AddAVProSource360LLTest()
        {
            return GetVideoManagerRoot() != null;
        }

        [MenuItem("GameObject/TXL/VideoTXL/Video Sources/Add AVPro 360p Low-Latency Source", false, 405)]
        public static void AddAVProSource360LL()
        {
            AddVideoSource("Packages/com.texelsaur.video/Runtime/Prefabs/Video Sources/AVPro360LL.prefab", GetVideoManagerRoot());
        }

        [MenuItem("GameObject/TXL/VideoTXL/Audio Profiles/Default Profile", true, 230)]
        public static bool AddDefaultProfileTest()
        {
            return GetAudioManagerRoot() != null;
        }

        [MenuItem("GameObject/TXL/VideoTXL/Audio Profiles/Default Profile", false, 230)]
        public static void AddDefaultProfile()
        {
            AddAudioProfile("Packages/com.texelsaur.video/Runtime/Prefabs/Audio Groups/Default.prefab", GetAudioManagerRoot());
        }

        [MenuItem("GameObject/TXL/VideoTXL/Audio Profiles/Global Profile", true, 231)]
        public static bool AddGlobalProfileTest()
        {
            return GetAudioManagerRoot() != null;
        }

        [MenuItem("GameObject/TXL/VideoTXL/Audio Profiles/Global Profile", false, 231)]
        public static void AddGlobalProfile()
        {
            AddAudioProfile("Packages/com.texelsaur.video/Runtime/Prefabs/Audio Groups/Global.prefab", GetAudioManagerRoot());
        }

        [MenuItem("GameObject/TXL/VideoTXL/Audio Profiles/Stereo Profile", true, 232)]
        public static bool AddStereoProfileTest()
        {
            return GetAudioManagerRoot() != null;
        }

        [MenuItem("GameObject/TXL/VideoTXL/Audio Profiles/Stereo Profile", false, 232)]
        public static void AddStereoProfile()
        {
            AddAudioProfile("Packages/com.texelsaur.video/Runtime/Prefabs/Audio Groups/Stereo.prefab", GetAudioManagerRoot());
        }

        [MenuItem("GameObject/TXL/VideoTXL/Audio Profiles/ARC - 5.1 Profile", true, 300)]
        public static bool AddARCProfileTest()
        {
            return GetAudioManagerRoot() != null;
        }

        [MenuItem("GameObject/TXL/VideoTXL/Audio Profiles/ARC - 5.1 Profile", false, 300)]
        public static void AddARCProfile()
        {
            AddAudioProfile("Packages/com.texelsaur.video/Runtime/Prefabs/Audio Groups/ARC.prefab", GetAudioManagerRoot());
        }

        [MenuItem("GameObject/TXL/VideoTXL/Audio Profiles/Default + AudioDMX Profile", true, 301)]
        public static bool AddAudioDMXProfileTest()
        {
            return GetAudioManagerRoot() != null;
        }

        [MenuItem("GameObject/TXL/VideoTXL/Audio Profiles/Default + AudioDMX Profile", false, 301)]
        public static void AddAudioDMXProfile()
        {
            AddAudioProfile("Packages/com.texelsaur.video/Runtime/Prefabs/Audio Groups/Default AudioDMX.prefab", GetAudioManagerRoot());
        }

        [MenuItem("GameObject/TXL/VideoTXL/Playlists and URL Sources/Playlist", false, 240)]
        public static void AddPlaylistToScene()
        {
            TXLVideoPlayer vp = GetVideoPlayer();
            if (vp)
            {
                GameObject playlistObj = MenuUtil.AddPrefabToObject("Packages/com.texelsaur.video/Runtime/Prefabs/Component/Playlist.prefab", vp.transform);
                if (vp is SyncPlayer && ((SyncPlayer)vp).urlSource == null)
                    ((SyncPlayer)vp).urlSource = playlistObj.GetComponent<Playlist>();
            } else
                MenuUtil.AddPrefabToScene("Packages/com.texelsaur.video/Runtime/Prefabs/Component/Playlist.prefab");
        }

        [MenuItem("GameObject/TXL/VideoTXL/Playlists and URL Sources/Playlist Data", false, 241)]
        public static void AddPlaylistDataToScene()
        {
            PlaylistCatalog catalog = MenuUtil.GetObjectOrParent<PlaylistCatalog>();
            if (catalog)
            {
                AddPlaylistData(catalog);
                return;
            }

            Playlist playlist = MenuUtil.GetObjectOrParent<Playlist>();
            if (playlist)
            {
                AddPlaylistData(playlist);
                return;
            }

            TXLVideoPlayer vp = GetVideoPlayer();
            if (vp)
            {
                AddPlaylistData(vp);
                return;
            }

            MenuUtil.AddPrefabToScene("Packages/com.texelsaur.video/Runtime/Prefabs/Component/PlaylistData.prefab");
        }

        static void AddPlaylistData(PlaylistCatalog catalog)
        {
            GameObject obj = MenuUtil.AddPrefabToObject("Packages/com.texelsaur.video/Runtime/Prefabs/Component/PlaylistData.prefab", catalog.transform);
            catalog.playlists = catalog.playlists.Append(obj.GetComponent<PlaylistData>()).ToArray();
        }

        static void AddPlaylistData(Playlist playlist)
        {
            if (playlist.playlistCatalog)
            {
                AddPlaylistData(playlist.playlistCatalog);
                return;
            }

            GameObject obj = MenuUtil.AddPrefabToObject("Packages/com.texelsaur.video/Runtime/Prefabs/Component/PlaylistData.prefab", playlist.transform);
            if (playlist.playlistData == null)
                playlist.playlistData = obj.GetComponent<PlaylistData>();
        }

        static void AddPlaylistData(TXLVideoPlayer vp)
        {
            if (vp is SyncPlayer && ((SyncPlayer)vp).urlSource is Playlist)
            {
                AddPlaylistData(((SyncPlayer)vp).urlSource as Playlist);
                return;
            }

            MenuUtil.AddPrefabToObject("Packages/com.texelsaur.video/Runtime/Prefabs/Component/PlaylistData.prefab", vp.transform);
        }

        [MenuItem("GameObject/TXL/VideoTXL/Playlists and URL Sources/Playlist Catalog", false, 242)]
        public static void AddPlaylistCatalogToScene()
        {
            Transform parent = null;
            Playlist playlist = MenuUtil.GetObjectOrParent<Playlist>();
            if (!playlist)
            {
                TXLVideoPlayer vp = GetVideoPlayer();
                if (vp)
                {
                    parent = vp.transform;
                    if (vp is SyncPlayer && ((SyncPlayer)vp).urlSource is Playlist)
                        playlist = ((SyncPlayer)vp).urlSource as Playlist;
                }
            }

            if (playlist)
                parent = playlist.transform;

            if (parent)
            {
                GameObject obj = MenuUtil.AddPrefabToObject("Packages/com.texelsaur.video/Runtime/Prefabs/Component/PlaylistCatalog.prefab", parent);
                PlaylistCatalog catalog = obj.GetComponent<PlaylistCatalog>();
                if (playlist)
                {
                    if (playlist.playlistCatalog == null)
                        playlist.playlistCatalog = catalog;
                    if (playlist.playlistData)
                        catalog.playlists = catalog.playlists.Append(playlist.playlistData).ToArray();
                }
                return;
            }

            MenuUtil.AddPrefabToScene("Packages/com.texelsaur.video/Runtime/Prefabs/Component/PlaylistCatalog.prefab");
        }

        [MenuItem("GameObject/TXL/VideoTXL/Components/Access Control", false, 250)]
        public static void AddAccessControlToScene()
        {
            TXLVideoPlayer vp = GetVideoPlayer();
            if (vp)
            {
                GameObject playlistObj = MenuUtil.AddPrefabToObject("Packages/com.texelsaur.common/Runtime/Prefabs/AccessControl.prefab", vp.transform);
                if (vp is SyncPlayer && ((SyncPlayer)vp).accessControl == null)
                    ((SyncPlayer)vp).accessControl = playlistObj.GetComponent<AccessControl>();
            }
            else
                MenuUtil.AddPrefabToScene("Packages/com.texelsaur.common/Runtime/Prefabs/AccessControl.prefab");
        }

        [MenuItem("GameObject/TXL/VideoTXL/Components/URL Remapper", false, 251)]
        public static void AddUrlRemapperToScene()
        {
            TXLVideoPlayer vp = GetVideoPlayer();
            if (vp)
            {
                GameObject playlistObj = MenuUtil.AddPrefabToObject("Packages/com.texelsaur.video/Runtime/Prefabs/Component/URL Remapper.prefab", vp.transform);
                if (vp is SyncPlayer && ((SyncPlayer)vp).urlRemapper == null)
                    ((SyncPlayer)vp).urlRemapper = playlistObj.GetComponent<UrlRemapper>();
            }
            else
                MenuUtil.AddPrefabToScene("Packages/com.texelsaur.video/Runtime/Prefabs/Component/URL Remapper.prefab");
        }

        [MenuItem("GameObject/TXL/VideoTXL/Components/Sync Playback Zone", false, 252)]
        public static void AddSyncPlaybackZoneToScene()
        {
            TXLVideoPlayer vp = GetVideoPlayer();
            if (vp)
            {
                GameObject playlistObj = MenuUtil.AddPrefabToObject("Packages/com.texelsaur.video/Runtime/Prefabs/Component/SyncPlaybackZone.prefab", vp.transform);
                if (vp is SyncPlayer && ((SyncPlayer)vp).playbackZoneMembership == null)
                    ((SyncPlayer)vp).playbackZoneMembership = playlistObj.GetComponent<ZoneMembership>();
            }
            else
                MenuUtil.AddPrefabToScene("Packages/com.texelsaur.video/Runtime/Prefabs/Component/SyncPlaybackZone.prefab");
        }

        [MenuItem("Tools/TXL/VideoTXL/Repair Prefabs in Scene")]
        public static void Repair()
        {
            Debug.Log("Starting automated repair");
            GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (GameObject root in roots)
            {
                RepairHierarchy<ScreenManager>(root);
                RepairHierarchy<AudioManager>(root);
                RepairHierarchy<VideoManager>(root);
                RepairHierarchy<VideoSource>(root);
                RepairHierarchy<VideoSourceAudioGroup>(root);
                RepairHierarchy<AudioChannelGroup>(root);
                RepairHierarchy<AudioChannel>(root);
                RepairHierarchy<SyncPlayer>(root);
                RepairHierarchy<LocalPlayer>(root);
                RepairHierarchy<PlayerControls>(root);
                RepairHierarchy<LocalControlsSlim>(root);

                VideoSourcePreprocess.ProcessHierarchy(root.transform);
            }
        }

        public static void RepairHierarchy<T>(GameObject root) where T : UdonSharpBehaviour
        {
            T[] coms = root.GetComponentsInChildren<T>();
            foreach (T com in coms)
            {
                UdonBehaviour backing = (UdonBehaviour)typeof(UdonSharpBehaviour).GetField("_udonSharpBackingUdonBehaviour", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(com);
                if (backing == null && PrefabUtility.IsPartOfPrefabInstance(com))
                {
                    SerializedObject so = new SerializedObject(com);
                    SerializedProperty prop = so.FindProperty("_udonSharpBackingUdonBehaviour");
                    Debug.Log($"Found missing backing behaviour on component {com}, attempting to fix");
                    PrefabUtility.RevertPropertyOverride(prop, InteractionMode.AutomatedAction);
                    if (prop.objectReferenceValue != null)
                        Debug.Log("Restored missing reference");
                }
            }
        }

        public static Transform GetVideoManagerRoot()
        {
            return MenuUtil.GetComponentRoot<TXLVideoPlayer, VideoManager>();
        }

        public static Transform GetAudioManagerRoot()
        {
            return MenuUtil.GetComponentRoot<TXLVideoPlayer, AudioManager>();
        }

        public static TXLVideoPlayer GetVideoPlayer()
        {
            return MenuUtil.GetObjectOrParent<TXLVideoPlayer>();
        }

        public static void AddVideoSource(string path, Transform parent)
        {
            GameObject source = MenuUtil.AddPrefabToObject(path, parent);
            if (!source)
                return;

            TXLVideoPlayer player = source.GetComponentInParent<TXLVideoPlayer>();
            if (!player)
                return;

            VideoComponentUpdater.UpdateComponents(player);
        }

        public static void AddAudioProfile(string path, Transform parent)
        {
            GameObject source = MenuUtil.AddPrefabToObject(path, parent);
            if (!source)
                return;

            TXLVideoPlayer player = source.GetComponentInParent<TXLVideoPlayer>();
            if (!player)
                return;

            VideoComponentUpdater.UpdateComponents(player);
        }
    }
}
