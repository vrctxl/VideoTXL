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
            Undo.SetCurrentGroupName("Add Controls");
            int undoGroup = Undo.GetCurrentGroup();

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
                        {
                            Undo.RecordObject(com, "Set References");
                            com.videoPlayer = vp;

                            AudioManager audioMan = vp.GetComponentInChildren<AudioManager>();
                            if (audioMan && audioMan.videoPlayer == vp)
                                com.audioManager = audioMan;

                            EditorUtility.SetDirty(com);
                        }

                        Undo.SetTransformParent(instance.transform, vp.transform, "Set Parent");
                        EditorUtility.SetDirty(instance.transform);
                        EditorUtility.SetDirty(vp.transform);
                    }
                }

                EditorGUIUtility.PingObject(instance);
            }

            Undo.CollapseUndoOperations(undoGroup);
        }

        [MenuItem("GameObject/TXL/VideoTXL/UI/Local Video Player Controls", false, 212)]
        public static void AddLocalPlayerControlsToScene()
        {
            Undo.SetCurrentGroupName("Add Local Controls");
            int undoGroup = Undo.GetCurrentGroup();

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
                        {
                            Undo.RecordObject(com, "Set References");
                            com.videoPlayer = vp;

                            AudioManager audioMan = vp.GetComponentInChildren<AudioManager>();
                            if (audioMan && audioMan.videoPlayer == vp)
                                com.audioManager = audioMan;

                            EditorUtility.SetDirty(com);
                        }

                        Undo.SetTransformParent(instance.transform, vp.transform, "Set Parent");
                        EditorUtility.SetDirty(instance.transform);
                        EditorUtility.SetDirty(vp.transform);
                    }
                }

                EditorGUIUtility.PingObject(instance);
            }

            Undo.CollapseUndoOperations(undoGroup);
        }

        [MenuItem("GameObject/TXL/VideoTXL/UI/Playlist UI", false, 300)]
        public static void AddPlaylistUIToScene()
        {
            Undo.SetCurrentGroupName("Add Playlist UI");
            int undoGroup = Undo.GetCurrentGroup();

            TXLVideoPlayer vp = GetVideoPlayer();
            if (vp)
            {
                if (vp is SyncPlayer && vp.SourceManager)
                {
                    Playlist found = vp.SourceManager.sources.FirstOrDefault(s => s is Playlist) as Playlist;
                    if (!found)
                    {
                        if (!EditorUtility.DisplayDialog("Playlist Not Found", "The associated video player is not currently configured with a playlist.  The prefab will need to be manually associated with a playlist.", "Place prefab anyway", "Cancel"))
                            return;
                    }

                    GameObject playlistObj = MenuUtil.AddPrefabToObject("Packages/com.texelsaur.video/Runtime/Prefabs/UI/Playlist UI.prefab", vp.transform);
                    PlaylistUI ui = playlistObj.GetComponentInChildren<PlaylistUI>();

                    if (found && ui)
                    {
                        Undo.RecordObject(ui, "Set Playlist");
                        ui.playlist = found;
                        EditorUtility.SetDirty(ui);
                    }
                }
            }
            else
                MenuUtil.AddPrefabToScene("Packages/com.texelsaur.video/Runtime/Prefabs/UI/PlaylistUI.prefab");

            Undo.CollapseUndoOperations(undoGroup);
        }

        [MenuItem("GameObject/TXL/VideoTXL/UI/Load Playlist Button", false, 301)]
        public static void AddPlaylistLoadButtonToScene()
        {
            Undo.SetCurrentGroupName("Add Playlist Load Button");
            int undoGroup = Undo.GetCurrentGroup();

            TXLVideoPlayer vp = GetVideoPlayer();
            if (vp)
            {
                GameObject playlistObj = MenuUtil.AddPrefabToObject("Packages/com.texelsaur.video/Runtime/Prefabs/UI/PlaylistLoadButton.prefab", vp.transform);
                if (vp is SyncPlayer && vp.SourceManager)
                {
                    Playlist found = vp.sourceManager.sources.FirstOrDefault(s => s is Playlist) as Playlist;
                    PlaylistLoadData ui = playlistObj.GetComponent<PlaylistLoadData>();

                    if (found && ui)
                    {
                        Undo.RecordObject(ui, "Set Playlist");
                        ui.playlist = found;
                        EditorUtility.SetDirty(ui);
                    }
                }
            }
            else
                MenuUtil.AddPrefabToScene("Packages/com.texelsaur.video/Runtime/Prefabs/UI/PlaylistLoadButton.prefab");

            Undo.CollapseUndoOperations(undoGroup);
        }

        [MenuItem("GameObject/TXL/VideoTXL/UI/Queue UI", false, 302)]
        public static void AddQueueUIToScene()
        {
            Undo.SetCurrentGroupName("Add Queue UI");
            int undoGroup = Undo.GetCurrentGroup();

            TXLVideoPlayer vp = GetVideoPlayer();
            if (vp)
            {
                if (vp is SyncPlayer && vp.SourceManager)
                {
                    PlaylistQueue found = vp.SourceManager.sources.FirstOrDefault(s => s is PlaylistQueue) as PlaylistQueue;
                    if (!found)
                    {
                        if (!EditorUtility.DisplayDialog("Queue Not Found", "The associated video player is not currently configured with a queue.  The prefab will need to be manually associated with a queue.", "Place prefab anyway", "Cancel"))
                            return;
                    }

                    GameObject playlistObj = MenuUtil.AddPrefabToObject("Packages/com.texelsaur.video/Runtime/Prefabs/UI/Queue UI.prefab", vp.transform);
                    PlaylistQueueUI queueUI = playlistObj.GetComponentInChildren<PlaylistQueueUI>();

                    if (found && queueUI)
                    {
                        Undo.RecordObject(queueUI, "Set Queue");
                        queueUI.queue = found;
                        EditorUtility.SetDirty(queueUI);
                    }
                }
            }
            else
                MenuUtil.AddPrefabToScene("Packages/com.texelsaur.video/Runtime/Prefabs/UI/Queue UI.prefab");

            Undo.CollapseUndoOperations(undoGroup);
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

        [MenuItem("GameObject/TXL/VideoTXL/Playlists and URL Sources/Source Manager", false, 240)]
        public static void AddSourceManagerToScene()
        {
            Undo.SetCurrentGroupName("Add URL Source Manager");
            int undoGroup = Undo.GetCurrentGroup();

            TXLVideoPlayer vp = GetVideoPlayer();
            if (vp)
            {
                GameObject managerObj = MenuUtil.AddPrefabToObject("Packages/com.texelsaur.video/Runtime/Prefabs/Component/Source Manager.prefab", vp.transform);
                if (vp is SyncPlayer && !vp.SourceManager)
                {
                    Undo.RecordObject(vp, "Set SourceManager Reference");
                    vp.sourceManager = managerObj.GetComponent<SourceManager>();

                    EditorUtility.SetDirty(vp);
                }
            }
            else
                MenuUtil.AddPrefabToScene("Packages/com.texelsaur.video/Runtime/Prefabs/Component/Source Manager.prefab");

            Undo.CollapseUndoOperations(undoGroup);
        }

        [MenuItem("GameObject/TXL/VideoTXL/Playlists and URL Sources/Playlist", false, 340)]
        public static void AddPlaylistToScene()
        {
            Undo.SetCurrentGroupName("Add Playlist");
            int undoGroup = Undo.GetCurrentGroup();

            TXLVideoPlayer vp = GetVideoPlayer();
            if (vp)
            {
                Transform target = vp.transform;
                if (vp.SourceManager)
                    target = vp.SourceManager.transform;

                GameObject playlistObj = MenuUtil.AddPrefabToObject("Packages/com.texelsaur.video/Runtime/Prefabs/Component/Playlist.prefab", target);
                if (vp is SyncPlayer && vp.SourceManager)
                {
                    Undo.RecordObject(vp.SourceManager, "Append Playlist");
                    vp.SourceManager.sources = vp.SourceManager.sources.Append(playlistObj.GetComponent<Playlist>()).ToArray();

                    EditorUtility.SetDirty(vp.SourceManager);
                }
            } else
                MenuUtil.AddPrefabToScene("Packages/com.texelsaur.video/Runtime/Prefabs/Component/Playlist.prefab");

            Undo.CollapseUndoOperations(undoGroup);
        }

        [MenuItem("GameObject/TXL/VideoTXL/Playlists and URL Sources/Playlist Data", false, 341)]
        public static void AddPlaylistDataToScene()
        {
            Undo.SetCurrentGroupName("Add Playlist Data");
            int undoGroup = Undo.GetCurrentGroup();

            PlaylistCatalog catalog = MenuUtil.GetObjectOrParent<PlaylistCatalog>();
            if (catalog)
            {
                AddPlaylistData(catalog);
                Undo.CollapseUndoOperations(undoGroup);
                return;
            }

            Playlist playlist = MenuUtil.GetObjectOrParent<Playlist>();
            if (playlist)
            {
                AddPlaylistData(playlist);
                Undo.CollapseUndoOperations(undoGroup);
                return;
            }

            TXLVideoPlayer vp = GetVideoPlayer();
            if (vp)
            {
                AddPlaylistData(vp);
                Undo.CollapseUndoOperations(undoGroup);
                return;
            }

            MenuUtil.AddPrefabToScene("Packages/com.texelsaur.video/Runtime/Prefabs/Component/PlaylistData.prefab");
            Undo.CollapseUndoOperations(undoGroup);
        }

        static void AddPlaylistData(PlaylistCatalog catalog)
        {
            GameObject obj = MenuUtil.AddPrefabToObject("Packages/com.texelsaur.video/Runtime/Prefabs/Component/PlaylistData.prefab", catalog.transform);

            Undo.RecordObject(catalog, "Update Catalog");
            catalog.playlists = catalog.playlists.Append(obj.GetComponent<PlaylistData>()).ToArray();
            EditorUtility.SetDirty(catalog);
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
            {
                Undo.RecordObject(playlist, "Update Playlist");
                playlist.playlistData = obj.GetComponent<PlaylistData>();
                EditorUtility.SetDirty(playlist);
            }
        }

        static void AddPlaylistData(TXLVideoPlayer vp)
        {
            if (vp is SyncPlayer && vp.SourceManager)
            {
                Playlist found = vp.SourceManager.sources.FirstOrDefault(s => s is Playlist) as Playlist;
                if (found)
                {
                    AddPlaylistData(found);
                    return;
                }
            }

            MenuUtil.AddPrefabToObject("Packages/com.texelsaur.video/Runtime/Prefabs/Component/PlaylistData.prefab", vp.transform);
        }

        [MenuItem("GameObject/TXL/VideoTXL/Playlists and URL Sources/Playlist Catalog", false, 342)]
        public static void AddPlaylistCatalogToScene()
        {
            Undo.SetCurrentGroupName("Add Playlist Catalog");
            int undoGroup = Undo.GetCurrentGroup();

            Transform parent = null;
            Playlist playlist = MenuUtil.GetObjectOrParent<Playlist>();
            if (!playlist)
            {
                TXLVideoPlayer vp = GetVideoPlayer();
                if (vp)
                {
                    parent = vp.transform;
                    if (vp is SyncPlayer && vp.SourceManager)
                    {
                        Playlist found = vp.SourceManager.sources.FirstOrDefault(s => s is Playlist) as Playlist;
                        if (found)
                            playlist = found;
                    }
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
                    {
                        Undo.RecordObject(playlist, "Update Playlist");
                        playlist.playlistCatalog = catalog;
                        EditorUtility.SetDirty(playlist);
                    }
                    if (playlist.playlistData)
                    {
                        Undo.RecordObject(catalog, "Update Catalog");
                        catalog.playlists = catalog.playlists.Append(playlist.playlistData).ToArray();
                        EditorUtility.SetDirty(catalog);
                    }
                }

                Undo.CollapseUndoOperations(undoGroup);
                return;
            }

            MenuUtil.AddPrefabToScene("Packages/com.texelsaur.video/Runtime/Prefabs/Component/PlaylistCatalog.prefab");
            Undo.CollapseUndoOperations(undoGroup);
        }

        [MenuItem("GameObject/TXL/VideoTXL/Playlists and URL Sources/Queue", false, 343)]
        public static void AddQueueToScene()
        {
            Undo.SetCurrentGroupName("Add Queue");
            int undoGroup = Undo.GetCurrentGroup();

            TXLVideoPlayer vp = GetVideoPlayer();
            if (vp)
            {
                Transform target = vp.transform;
                if (vp.SourceManager)
                    target = vp.SourceManager.transform;

                GameObject playlistObj = MenuUtil.AddPrefabToObject("Packages/com.texelsaur.video/Runtime/Prefabs/Component/Queue.prefab", target);
                if (vp is SyncPlayer && vp.SourceManager)
                {
                    Undo.RecordObject(vp.SourceManager, "Append Queue");
                    vp.SourceManager.sources = vp.SourceManager.sources.Append(playlistObj.GetComponent<PlaylistQueue>()).ToArray();
                    EditorUtility.SetDirty(vp.SourceManager);
                }
            }
            else
                MenuUtil.AddPrefabToScene("Packages/com.texelsaur.video/Runtime/Prefabs/Component/Queue.prefab");

            Undo.CollapseUndoOperations(undoGroup);
        }

        [MenuItem("GameObject/TXL/VideoTXL/Components/Access Control", false, 250)]
        public static void AddAccessControlToScene()
        {
            Undo.SetCurrentGroupName("Add Access Control");
            int undoGroup = Undo.GetCurrentGroup();

            TXLVideoPlayer vp = GetVideoPlayer();
            if (vp)
            {
                GameObject playlistObj = MenuUtil.AddPrefabToObject("Packages/com.texelsaur.common/Runtime/Prefabs/AccessControl.prefab", vp.transform);
                if (vp is SyncPlayer && ((SyncPlayer)vp).accessControl == null)
                {
                    Undo.RecordObject(vp, "Set Access Control");
                    ((SyncPlayer)vp).accessControl = playlistObj.GetComponent<AccessControl>();
                    EditorUtility.SetDirty(vp);
                }
            }
            else
                MenuUtil.AddPrefabToScene("Packages/com.texelsaur.common/Runtime/Prefabs/AccessControl.prefab");

            Undo.CollapseUndoOperations(undoGroup);
        }

        [MenuItem("GameObject/TXL/VideoTXL/Components/URL Remapper", false, 251)]
        public static void AddUrlRemapperToScene()
        {
            Undo.SetCurrentGroupName("Add URL Remapper");
            int undoGroup = Undo.GetCurrentGroup();

            TXLVideoPlayer vp = GetVideoPlayer();
            if (vp)
            {
                GameObject playlistObj = MenuUtil.AddPrefabToObject("Packages/com.texelsaur.video/Runtime/Prefabs/Component/URL Remapper.prefab", vp.transform);
                if (vp is SyncPlayer && ((SyncPlayer)vp).urlRemapper == null)
                {
                    Undo.RecordObject(vp, "Set URL Remapper");
                    ((SyncPlayer)vp).urlRemapper = playlistObj.GetComponent<UrlRemapper>();
                    EditorUtility.SetDirty(vp);
                }
            }
            else
                MenuUtil.AddPrefabToScene("Packages/com.texelsaur.video/Runtime/Prefabs/Component/URL Remapper.prefab");

            Undo.CollapseUndoOperations(undoGroup);
        }

        [MenuItem("GameObject/TXL/VideoTXL/Components/URL Info Resolver", false, 252)]
        public static void AddUrlInfoResolverToScene()
        {
            Undo.SetCurrentGroupName("Add URL Info Reoslver");
            int undoGroup = Undo.GetCurrentGroup();

            TXLVideoPlayer vp = GetVideoPlayer();
            if (vp)
            {
                GameObject resolverObj = MenuUtil.AddPrefabToObject("Packages/com.texelsaur.video/Runtime/Prefabs/Component/URL Info Resolver.prefab", vp.transform);
                if (vp is SyncPlayer && ((SyncPlayer)vp).urlInfoResolver == null)
                {
                    Undo.RecordObject(vp, "Set URL Info Resolver");
                    ((SyncPlayer)vp).urlInfoResolver = resolverObj.GetComponent<UrlInfoResolver>();
                    EditorUtility.SetDirty(vp);
                }
            }
            else
                MenuUtil.AddPrefabToScene("Packages/com.texelsaur.video/Runtime/Prefabs/Component/URL Info Resolver.prefab");

            Undo.CollapseUndoOperations(undoGroup);
        }

        [MenuItem("GameObject/TXL/VideoTXL/Components/Dependent Source", false, 253)]
        public static void AddDependentSourceToScene()
        {
            Undo.SetCurrentGroupName("Add URL Info Reoslver");
            int undoGroup = Undo.GetCurrentGroup();

            TXLVideoPlayer vp = GetVideoPlayer();
            if (vp)
            {
                GameObject playlistObj = MenuUtil.AddPrefabToObject("Packages/com.texelsaur.video/Runtime/Prefabs/Component/Dependent Source.prefab", vp.transform);
                if (vp is LocalPlayer && ((LocalPlayer)vp).urlRemapper == null)
                {
                    Undo.RecordObject(vp, "Set Dependent Source");
                    ((LocalPlayer)vp).dependentSource = playlistObj.GetComponent<DependentSource>();
                    EditorUtility.SetDirty(vp);
                }
            }
            else
                MenuUtil.AddPrefabToScene("Packages/com.texelsaur.video/Runtime/Prefabs/Component/Dependent Source.prefab");

            Undo.CollapseUndoOperations(undoGroup);
        }

        [MenuItem("GameObject/TXL/VideoTXL/Components/Sync Playback Zone", false, 254)]
        public static void AddSyncPlaybackZoneToScene()
        {
            Undo.SetCurrentGroupName("Add Sync Playback Zone");
            int undoGroup = Undo.GetCurrentGroup();

            TXLVideoPlayer vp = GetVideoPlayer();
            if (vp)
            {
                GameObject playlistObj = MenuUtil.AddPrefabToObject("Packages/com.texelsaur.video/Runtime/Prefabs/Component/SyncPlaybackZone.prefab", vp.transform);
                if (vp is SyncPlayer && ((SyncPlayer)vp).trackedZoneTrigger == null)
                {
                    Undo.RecordObject(vp, "Set Dependent Source");
                    ((SyncPlayer)vp).trackedZoneTrigger = playlistObj.GetComponent<TrackedZoneTrigger>();
                    EditorUtility.SetDirty(vp);
                }
            }
            else
                MenuUtil.AddPrefabToScene("Packages/com.texelsaur.video/Runtime/Prefabs/Component/SyncPlaybackZone.prefab");

            Undo.CollapseUndoOperations(undoGroup);
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
            Undo.SetCurrentGroupName("Add Video Source");
            int undoGroup = Undo.GetCurrentGroup();

            GameObject source = MenuUtil.AddPrefabToObject(path, parent);
            if (!source)
                return;

            TXLVideoPlayer player = source.GetComponentInParent<TXLVideoPlayer>();
            if (!player)
                return;

            VideoComponentUpdater.UpdateComponents(player);

            Undo.CollapseUndoOperations(undoGroup);
        }

        public static void AddAudioProfile(string path, Transform parent)
        {
            Undo.SetCurrentGroupName("Add Audio Profile");
            int undoGroup = Undo.GetCurrentGroup();

            GameObject source = MenuUtil.AddPrefabToObject(path, parent);
            if (!source)
                return;

            TXLVideoPlayer player = source.GetComponentInParent<TXLVideoPlayer>();
            if (!player)
                return;

            VideoComponentUpdater.UpdateComponents(player);

            Undo.CollapseUndoOperations(undoGroup);
        }
    }
}
