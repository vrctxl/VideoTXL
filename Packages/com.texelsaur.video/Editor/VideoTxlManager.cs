using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Texel
{
    [InitializeOnLoad]
    public class VideoTxlManager
    {
        [MenuItem("Tools/TXL/VideoTXL/Add \"Sync Video Player\" Prefab to Scene", false)]
        [MenuItem("GameObject/TXL/VideoTXL", false, 49)]
        [MenuItem("GameObject/TXL/VideoTXL/Sync Video Player", false, 100)]
        public static void AddSyncPlayerToScene()
        {
            AddPrefabToScene("Packages/com.texelsaur.video/Runtime/Prefabs/Sync Video Player.prefab");
        }

        [MenuItem("GameObject/TXL/VideoTXL/Other Video Players", false, 200)]
        [MenuItem("GameObject/TXL/VideoTXL/Other Video Players/Basic Sync Video Player", false, 201)]
        public static void AddBasicSyncPlayerToScene()
        {
            AddPrefabToScene("Packages/com.texelsaur.video/Runtime/Prefabs/Other Video Players/Basic Sync Video Player.prefab");
        }

        [MenuItem("GameObject/TXL/VideoTXL/Other Video Players/Local Video Player AVPro", false, 202)]
        public static void AddLocalAVProPlayerToScene()
        {
            AddPrefabToScene("Packages/com.texelsaur.video/Runtime/Prefabs/Other Video Players/Local Video Player AVPro.prefab");
        }

        [MenuItem("GameObject/TXL/VideoTXL/Other Video Players/Local Video Player Unity", false, 203)]
        public static void AddLocalUnityPlayerToScene()
        {
            AddPrefabToScene("Packages/com.texelsaur.video/Runtime/Prefabs/Other Video Players/Local Video Player Unity.prefab");
        }

        [MenuItem("GameObject/TXL/VideoTXL/Other Video Players/Sync Video Player Full", false, 204)]
        public static void AddSyncPlayerFullToScene()
        {
            AddPrefabToScene("Packages/com.texelsaur.video/Runtime/Prefabs/Other Video Players/Sync Video Player Full.prefab");
        }

        [MenuItem("GameObject/TXL/VideoTXL/UI", false, 210)]
        [MenuItem("GameObject/TXL/VideoTXL/UI/Sync Video Player Controls", false, 211)]
        public static void AddSyncPlayerControlsToScene()
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.texelsaur.video/Runtime/UI/Sync Player/PlayerControls.prefab");
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
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.texelsaur.video/Runtime/UI/Local Controls/LocalControlsSlim.prefab");
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

        public static void AddPrefabToScene(string path)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                EditorGUIUtility.PingObject(instance);
            }
        }
    }
}
