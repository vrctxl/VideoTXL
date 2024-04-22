
using System;
using System.Runtime.CompilerServices;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Image;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

[assembly: InternalsVisibleTo("com.texelsaur.video.Editor")]

namespace Texel
{
    public enum ScreenOverrideType
    {
        Playback = 0,
        Logo = 1,
        Loading = 2,
        Sync = 3,
        Audio = 4,
        Error = 5,
        ErrorInvalid = 6,
        ErrorRate = 7,
        ErrorBlocked = 8,
        Editor = 9,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ScreenManager : EventBase
    {
        [Tooltip("A proxy for dispatching video-related events to this object")]
        [SerializeField] internal TXLVideoPlayer videoPlayer;

        [Tooltip("Log debug statements to a world object")]
        [SerializeField] internal DebugLog debugLog;
        [SerializeField] internal DebugState debugState;
        [SerializeField] internal bool vrcLogging = false;
        [SerializeField] internal bool eventLogging = false;
        [SerializeField] internal bool lowLevelLogging = false;

        [Tooltip("Prevent screen state from cycling between loading and error placeholders.")]
        [SerializeField] internal bool latchErrorState = true;

        bool useMaterialOverrides = true;
        [Tooltip("The screen material to apply when video is playing back.  The object's current material is captured as the playback material by default.")]
        [SerializeField] internal Material playbackMaterial;
        [Tooltip("The screen material to apply when no video is playing or loading.")]
        [SerializeField] internal Material logoMaterial;
        [Tooltip("The screen material to apply when a video is being loaded.  Falls back to Logo Material.")]
        [SerializeField] internal Material loadingMaterial;
        [SerializeField] internal Material syncMaterial;
        [Tooltip("The screen material to apply when an audio-only video is detected.")]
        [SerializeField] internal Material audioMaterial;
        [Tooltip("The screen material to apply when an error has occurred.  Falls back to Logo Material.")]
        [SerializeField] internal Material errorMaterial;
        [Tooltip("The screen material to apply when an invalid URL or offline stream has been loaded.  Falls back to Error Material.")]
        [SerializeField] internal Material errorInvalidMaterial;
        [Tooltip("The screen material to apply when loading has been temporarily rate-limited.  Falls back to Error Material.")]
        [SerializeField] internal Material errorRateLimitedMaterial;
        [Tooltip("The screen material to apply when an untrusted URL has been loaded and untrusted URLs are blocked.  Falls back to Error Material.")]
        [SerializeField] internal Material errorBlockedMaterial;

        [SerializeField] internal MeshRenderer[] screenMesh;
        [SerializeField] internal int[] screenMaterialIndex;

        bool useTextureOverrides = true;

        [Tooltip("Whether the aspect ratio for placeholder textures should be overridden by a specific value.  Use this to supply the original aspect ratio for textures that have been rescaled to powers-of-2.")]
        [SerializeField] internal bool overrideAspectRatio = true;
        [Tooltip("The aspect ratio that should be used for placeholder textures, ignoring their native unity value.")]
        [SerializeField] internal float aspectRatio = 1.777f;

        [Tooltip("The screen texture to apply when no video is playing or loading.")]
        [SerializeField] internal Texture logoTexture;
        [Tooltip("The screen texture to apply when a video is being loaded.  Falls back to Logo Texture.")]
        [SerializeField] internal Texture loadingTexture;
        [SerializeField] internal Texture syncTexture;
        [Tooltip("The screen texture to apply when an audio-only video is detected.")]
        [SerializeField] internal Texture audioTexture;
        [Tooltip("The screen texture to apply when an error has occurred.  Falls back to Logo Texture.")]
        [SerializeField] internal Texture errorTexture;
        [Tooltip("The screen texture to apply when an invalid URL or offline stream has been loaded.  Falls back to Error Texture.")]
        [SerializeField] internal Texture errorInvalidTexture;
        [Tooltip("The screen texture to apply when loading has been temporarily rate-limited.  Falls back to Error Texture.")]
        [SerializeField] internal Texture errorRateLimitedTexture;
        [Tooltip("The screen texture to apply when an untrusted URL has been loaded and untrusted URLs are blocked.  Falls back to Error Texture.")]
        [SerializeField] internal Texture errorBlockedTexture;

        [Tooltip("The screen texture to apply in Unity's editor runtime")]
        [SerializeField] internal Texture editorTexture;

        [SerializeField] internal Material[] materialUpdateList;
        [SerializeField] internal ScreenPropertyMap[] materialPropertyList;

        [SerializeField] internal MeshRenderer[] propMeshList;
        [SerializeField] internal int[] propMaterialOverrideList;
        [SerializeField] internal int[] propMaterialIndexList;
        [SerializeField] internal ScreenPropertyMap[] propPropertyList;

        [SerializeField] internal ScreenPropertyMap[] globalPropertyList;

        [SerializeField] internal bool useRenderOut = false;
        [SerializeField] internal CustomRenderTexture outputCRT;
        [SerializeField] internal ScreenPropertyMap outputMaterialProperties;

        [SerializeField] internal CustomRenderTexture[] renderOutCrt;
        [SerializeField] internal ScreenPropertyMap[] renderOutMatProps;
        [SerializeField] internal Vector2Int[] renderOutSize;
        [SerializeField] internal float[] renderOutTargetAspect;
        [SerializeField] internal bool[] renderOutResize;
        [SerializeField] internal bool[] renderOutExpandSize;
        [SerializeField] internal bool[] renderOutGlobalTex;

        [SerializeField] internal bool downloadLogoImage;
        [SerializeField] internal VRCUrl downloadLogoImageUrl;

        int baseIndexCrt;
        int shaderPropCrtLength;
        int baseIndexMat;
        int shaderPropMatLength;
        int baseIndexProp;
        int shaderPropPropLength;
        int baseIndexGlobal;
        int shaderPropGlobalLength;

        string[] shaderPropMainTexList;
        string[] shaderPropAVProList;
        string[] shaderPropInvertList;
        string[] shaderPropGammaList;
        string[] shaderPropFitList;
        string[] shaderPropAspectRatioList;

        int[] globalPropMainTexList;
        int[] globalPropAVProList;
        int[] globalPropInvertList;
        int[] globalPropGammaList;
        int[] globalPropFitList;
        int[] globalPropAspectRatioList;

        Material[] _originalScreenMaterial;
        Texture[] _originalMaterialTexture;
        int[] _originalMatAVPro;
        int[] _originalMatInvert;
        int[] _originalMatGamma;
        int[] _originalMatFit;
        float[] _originalMatAspectRatio;

        public const int SCREEN_MODE_UNINITIALIZED = -1;
        public const int SCREEN_MODE_NORMAL = 0;
        public const int SCREEN_MODE_LOGO = 1;
        public const int SCREEN_MODE_LOADING = 2;
        public const int SCREEN_MODE_ERROR = 3;
        public const int SCREEN_MODE_AUDIO = 4;
        public const int SCREEN_MODE_SYNC = 5;

        const int SCREEN_INDEX_PLAYBACK = 0;
        public const int SCREEN_INDEX_LOGO = 1;
        public const int SCREEN_INDEX_LOADING = 2;
        public const int SCREEN_INDEX_SYNC = 3;
        public const int SCREEN_INDEX_AUDIO = 4;
        public const int SCREEN_INDEX_ERROR = 5;
        public const int SCREEN_INDEX_ERROR_INVALID = 6;
        public const int SCREEN_INDEX_ERROR_RATE = 7;
        public const int SCREEN_INDEX_ERROR_BLOCKED = 8;
        const int SCREEN_INDEX_EDITOR = 9;
        const int SCREEN_INDEX_COUNT = 10;

        int[] screenIndexFallbackMap;

        Material[] replacementMaterials;
        Texture[] replacementTextures;

        MeshRenderer captureRenderer;
        int _screenSource = VideoSource.VIDEO_SOURCE_UNITY;
        int _screenMode = SCREEN_MODE_UNINITIALIZED;
        int _screenFit = 0;
        bool _inError = false;
        VideoError _lastErrorCode = 0;
        int _checkFrameCount = 0;
        MaterialPropertyBlock block;
        int pendingUpdates = 0;

        Texture currentTexture;
        Texture captureTexture;
        bool currentAVPro;
        bool currentInvert;
        bool currentGamma;
        int currentFit;
        float currentAspectRatio;
        bool currentValid = false;
        Vector2Int currentRes;

        int globalTexPropertyId = -1;

        VRCImageDownloader imageDownloader;

        const int EVENT_UPDATE = 0;
        const int EVENT_CAPTURE_VALID = 1;
        const int EVENT_COUNT = 2;

        void Start()
        {
            _EnsureInit();
        }

        protected override int EventCount { get => EVENT_COUNT; }

        protected override void _Init()
        {
            _DebugLog("Init");

            if (!videoPlayer)
            {
                _DebugError($"Screen manager has no associated video player.");
                videoPlayer = gameObject.transform.parent.GetComponentInParent<TXLVideoPlayer>();
                if (videoPlayer)
                    _DebugLog($"Found video player on parent: {videoPlayer.gameObject.name}");
                else
                    _DebugError("Could not find parent video player.  Video playback will not work.", true);
            }

            block = new MaterialPropertyBlock();

            screenIndexFallbackMap = new int[SCREEN_INDEX_COUNT];
            screenIndexFallbackMap[SCREEN_INDEX_PLAYBACK] = -1;
            screenIndexFallbackMap[SCREEN_INDEX_LOGO] = -1;
            screenIndexFallbackMap[SCREEN_INDEX_LOADING] = SCREEN_INDEX_LOGO;
            screenIndexFallbackMap[SCREEN_INDEX_SYNC] = SCREEN_INDEX_LOGO;
            screenIndexFallbackMap[SCREEN_INDEX_AUDIO] = SCREEN_INDEX_LOGO;
            screenIndexFallbackMap[SCREEN_INDEX_ERROR] = SCREEN_INDEX_LOGO;
            screenIndexFallbackMap[SCREEN_INDEX_ERROR_INVALID] = SCREEN_INDEX_ERROR;
            screenIndexFallbackMap[SCREEN_INDEX_ERROR_RATE] = SCREEN_INDEX_ERROR;
            screenIndexFallbackMap[SCREEN_INDEX_ERROR_BLOCKED] = SCREEN_INDEX_ERROR;
            screenIndexFallbackMap[SCREEN_INDEX_EDITOR] = -1;

            replacementMaterials = new Material[SCREEN_INDEX_COUNT];
            replacementMaterials[SCREEN_INDEX_PLAYBACK] = null;
            replacementMaterials[SCREEN_INDEX_LOGO] = logoMaterial;
            replacementMaterials[SCREEN_INDEX_LOADING] = loadingMaterial ?? logoMaterial;
            replacementMaterials[SCREEN_INDEX_SYNC] = syncMaterial ?? logoMaterial;
            replacementMaterials[SCREEN_INDEX_AUDIO] = audioMaterial ?? logoMaterial;
            replacementMaterials[SCREEN_INDEX_ERROR] = errorMaterial ?? logoMaterial;
            replacementMaterials[SCREEN_INDEX_ERROR_INVALID] = errorInvalidMaterial ?? replacementMaterials[SCREEN_INDEX_ERROR];
            replacementMaterials[SCREEN_INDEX_ERROR_RATE] = errorRateLimitedMaterial ?? replacementMaterials[SCREEN_INDEX_ERROR];
            replacementMaterials[SCREEN_INDEX_ERROR_BLOCKED] = errorBlockedMaterial ?? replacementMaterials[SCREEN_INDEX_ERROR];
            replacementMaterials[SCREEN_INDEX_EDITOR] = null;

            replacementTextures = new Texture[SCREEN_INDEX_COUNT];
            replacementTextures[SCREEN_INDEX_PLAYBACK] = null;
            replacementTextures[SCREEN_INDEX_LOGO] = logoTexture;
            replacementTextures[SCREEN_INDEX_LOADING] = loadingTexture;
            replacementTextures[SCREEN_INDEX_SYNC] = syncTexture;
            replacementTextures[SCREEN_INDEX_AUDIO] = audioTexture;
            replacementTextures[SCREEN_INDEX_ERROR] = errorTexture;
            replacementTextures[SCREEN_INDEX_ERROR_INVALID] = errorInvalidTexture;
            replacementTextures[SCREEN_INDEX_ERROR_RATE] = errorRateLimitedTexture;
            replacementTextures[SCREEN_INDEX_ERROR_BLOCKED] = errorBlockedTexture;
            replacementTextures[SCREEN_INDEX_EDITOR] = editorTexture;

            _InitGlobalTex();

#if COMPILER_UDONSHARP
            _InitMaterialOverrides();
            _InitTextureOverrides();
#endif

            if (downloadLogoImage)
            {
                imageDownloader = new VRCImageDownloader();

                TextureInfo info = new TextureInfo();
                info.GenerateMipMaps = true;

                imageDownloader.DownloadImage(downloadLogoImageUrl, null, (IUdonEventReceiver)this, info);
            }

            if (Utilities.IsValid(debugState))
                _SetDebugState(debugState);

            if (eventLogging)
                eventDebugLog = debugLog;
        }

        protected override void _PostInit()
        {
            _BindVideoPlayer(videoPlayer);
        }

        public void _BindVideoPlayer(TXLVideoPlayer videoPlayer)
        {
            TXLVideoPlayer prevPlayer = this.videoPlayer;
            if (prevPlayer)
            {
                prevPlayer._Unregister(TXLVideoPlayer.EVENT_VIDEO_STATE_UPDATE, this, nameof(_InternalOnVideoStateUpdate));
                prevPlayer._Unregister(TXLVideoPlayer.EVENT_VIDEO_SOURCE_CHANGE, this, nameof(_InternalOnSourceChanged));
            }

            captureRenderer = null;
            _screenSource = VideoSource.VIDEO_SOURCE_NONE;

            this.videoPlayer = videoPlayer;
            if (videoPlayer)
            {
                videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_STATE_UPDATE, this, nameof(_InternalOnVideoStateUpdate));
                videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_SOURCE_CHANGE, this, nameof(_InternalOnSourceChanged));
            }

            _InternalOnSourceChanged();
            _InternalOnVideoStateUpdate();
        }

        public override void OnImageLoadSuccess(IVRCImageDownload result)
        {
            _SetTextureOverride(ScreenOverrideType.Logo, result.Result);
            _ResetCheckScreenMaterial();
        }

        private void OnDisable()
        {

#if COMPILER_UDONSHARP
            _RestoreMaterialOverrides();
            _RestoreTextureOverrides();
#endif
        }

        private void OnDestroy()
        {
            if (imageDownloader != null)
            {
                imageDownloader.Dispose();
                imageDownloader = null;
            }
        }

        public Texture CurrentTexture
        {
            get { return currentTexture; }
        }

        public bool CaptureIsAVPro
        {
            get { return currentAVPro; }
        }

        public bool CaptureNeedsInvert
        {
            get { return currentInvert; }
        }

        public bool CaptureNeedsApplyGamma
        {
            get { return currentGamma; }
        }

        public TXLScreenFit CurrentScreenFit
        {
            get { return (TXLScreenFit)currentFit; }
        }

        public float CurrentAspectRatioOverride
        {
            get { return currentAspectRatio; }
        }

        void _InitMaterialOverrides()
        {
            useMaterialOverrides = true;

            if (!Utilities.IsValid(screenMesh) || screenMesh.Length == 0)
            {
                useMaterialOverrides = false;
                return;
            }

            // Capture original screen materials
            _originalScreenMaterial = new Material[screenMesh.Length];
            for (int i = 0; i < screenMesh.Length; i++)
            {
                if (screenMesh[i] == null)
                {
                    screenMaterialIndex[i] = -1;
                    continue;
                }

                int index = screenMaterialIndex[i];
                Material[] materials = screenMesh[i].sharedMaterials;
                if (index < 0 || index >= materials.Length)
                {
                    screenMaterialIndex[i] = -1;
                    continue;
                }

                _originalScreenMaterial[i] = materials[index];
            }
        }

        void _InitTextureOverrides()
        {
            useTextureOverrides = true;

            bool hasMaterialUpdates = Utilities.IsValid(materialUpdateList) && materialUpdateList.Length > 0;
            bool hasPropupdates = Utilities.IsValid(propMeshList) && propMeshList.Length > 0;
            bool hasCrtUpdates = Utilities.IsValid(renderOutCrt) && renderOutCrt.Length > 0;
            bool hasGlobalUpdates = Utilities.IsValid(globalPropertyList) && globalPropertyList.Length > 0;

            if (!hasMaterialUpdates && !hasPropupdates && !hasCrtUpdates && !hasGlobalUpdates)
            {
                useTextureOverrides = false;
                return;
            }

            // Legacy upgrade
            if (useRenderOut && !hasCrtUpdates)
            {
                renderOutCrt = new CustomRenderTexture[1];
                renderOutMatProps = new ScreenPropertyMap[1];
                renderOutSize = new Vector2Int[1];
                renderOutTargetAspect = new float[1];
                renderOutResize = new bool[1];
                renderOutExpandSize = new bool[1];

                renderOutCrt[0] = outputCRT;
                renderOutMatProps[0] = outputMaterialProperties;
                renderOutSize[0] = outputCRT ? new Vector2Int(outputCRT.width, outputCRT.height) : Vector2Int.zero;
                renderOutTargetAspect[0] = 0;
                renderOutResize[0] = false;
                renderOutExpandSize[0] = false;

                hasCrtUpdates = true;
            }

            baseIndexCrt = 0;
            shaderPropCrtLength = renderOutCrt.Length;
            baseIndexMat = baseIndexCrt + shaderPropCrtLength;
            shaderPropMatLength = materialUpdateList.Length;
            baseIndexProp = baseIndexMat + shaderPropMatLength;
            shaderPropPropLength = propMeshList.Length;

            int totalPropLength = shaderPropCrtLength + shaderPropMatLength + shaderPropPropLength;
            shaderPropMainTexList = new string[totalPropLength];
            shaderPropAVProList = new string[totalPropLength];
            shaderPropInvertList = new string[totalPropLength];
            shaderPropGammaList = new string[totalPropLength];
            shaderPropFitList = new string[totalPropLength];
            shaderPropAspectRatioList = new string[totalPropLength];

            // Material Props
            if (hasMaterialUpdates)
            {
                for (int i = 0; i < materialUpdateList.Length; i++)
                {
                    if (materialUpdateList[i] == null)
                        continue;

                    ScreenPropertyMap propMap = materialPropertyList[i];
                    if (propMap)
                        _LoadPropertyMap(baseIndexMat + i, propMap);
                    else
                        _TryLoadDefaultProps(baseIndexMat + i, materialUpdateList[i]);
                }
            }

            // Property Block Props
            if (hasPropupdates)
            {
                for (int i = 0; i < propMeshList.Length; i++)
                {
                    if (propMeshList[i] == null)
                        continue;


                    ScreenPropertyMap propMap = propPropertyList[i];
                    if (propMap)
                        _LoadPropertyMap(baseIndexProp + i, propMap);
                    else
                    {
                        Material[] mats = propMeshList[i].sharedMaterials;
                        bool useMatIndex = propMaterialOverrideList[i] == 1;
                        int matIndex = propMaterialIndexList[i];

                        if (useMatIndex && matIndex < mats.Length && mats[matIndex])
                            _TryLoadDefaultProps(baseIndexProp + i, mats[matIndex]);
                        else if (!useMatIndex)
                        {
                            for (int j = 0; j < mats.Length; j++)
                            {
                                if (_TryLoadDefaultProps(baseIndexProp + i, mats[j]))
                                    break;
                            }
                        }
                    }
                }
            }

            // Output Material
            if (hasCrtUpdates)
            {
                for (int i = 0; i < renderOutCrt.Length; i++)
                {
                    if (renderOutCrt[i] == null)
                        continue;

                    ScreenPropertyMap propMap = renderOutMatProps[i];
                    if (propMap)
                        _LoadPropertyMap(baseIndexCrt + i, propMap);
                    else
                        _TryLoadDefaultProps(baseIndexCrt + i, renderOutCrt[i].material);
                }
            }

            // Global Shader Props
            int globalLength = globalPropertyList.Length;
            globalPropMainTexList = new int[globalLength];
            globalPropAVProList = new int[globalLength];
            globalPropInvertList = new int[globalLength];
            globalPropGammaList = new int[globalLength];
            globalPropFitList = new int[globalLength];
            globalPropAspectRatioList = new int[globalLength];

            if (hasGlobalUpdates)
            {
                for (int i = 0; i < globalPropertyList.Length; i++)
                {
                    if (globalPropertyList[i] == null)
                        continue;

                    _LoadGlobalPropertyMap(i, globalPropertyList[i]);
                }
            }

            // Capture original material textures
            _originalMaterialTexture = new Texture[materialUpdateList.Length];
            _originalMatAVPro = new int[materialUpdateList.Length];
            _originalMatInvert = new int[materialUpdateList.Length];
            _originalMatGamma = new int[materialUpdateList.Length];
            _originalMatFit = new int[materialUpdateList.Length];
            _originalMatAspectRatio = new float[materialUpdateList.Length];

            for (int i = 0; i < materialUpdateList.Length; i++)
            {
                if (materialUpdateList[i] == null)
                    continue;

                string name = shaderPropMainTexList[baseIndexMat + i];
                if (name == null || name.Length == 0)
                    continue;

                _originalMaterialTexture[i] = materialUpdateList[i].GetTexture(name);
                _originalMatAVPro[i] = _GetMatIntProperty(materialUpdateList[i], shaderPropAVProList[baseIndexMat + i]);
                _originalMatInvert[i] = _GetMatIntProperty(materialUpdateList[i], shaderPropInvertList[baseIndexMat + i]);
                _originalMatGamma[i] = _GetMatIntProperty(materialUpdateList[i], shaderPropGammaList[baseIndexMat + i]);
                _originalMatFit[i] = _GetMatIntProperty(materialUpdateList[i], shaderPropFitList[baseIndexMat + i]);
                _originalMatAspectRatio[i] = _GetMatFloatProperty(materialUpdateList[i], shaderPropAspectRatioList[baseIndexMat + i]);
            }
        }

        void _InitGlobalTex()
        {
            if (renderOutCrt == null || renderOutGlobalTex == null)
                return;

            globalTexPropertyId = VRCShader.PropertyToID("_Udon_VideoTex");

            int count = Math.Min(renderOutCrt.Length, renderOutGlobalTex.Length);
            for (int i = 0; i < count; i++)
            {
                if (renderOutCrt[i] && renderOutGlobalTex[i])
                {
                    VRCShader.SetGlobalTexture(globalTexPropertyId, renderOutCrt[i]);
                    break;
                }
            }
        }

        bool _TryLoadDefaultProps(int i, Material mat)
        {
            if (!mat || !mat.shader)
                return false;

            switch (mat.shader.name)
            {
                case "VideoTXL/RealtimeEmissiveGamma":
                case "VideoTXL/Unlit":
                    _LoadRealtimeEmissiveGammaProps(i);
                    return true;
                case "VideoTXL/RenderOut":
                    _LoadRenderOutProps(i);
                    return true;
                default:
                    return false;
            }
        }

        void _LoadPropertyMap(int i, ScreenPropertyMap propMap)
        {
            if (!propMap)
                return;

            shaderPropMainTexList[i] = propMap.screenTexture;
            shaderPropAVProList[i] = propMap.avProCheck;
            shaderPropInvertList[i] = propMap.invertY;
            shaderPropGammaList[i] = propMap.applyGamma;
            shaderPropFitList[i] = propMap.screenFit;
            shaderPropAspectRatioList[i] = propMap.aspectRatio;
        }

        void _LoadGlobalPropertyMap(int i, ScreenPropertyMap propMap)
        {
            if (!propMap)
                return;

            _ResolvePropertyId(globalPropMainTexList, i, propMap.screenTexture);
            _ResolvePropertyId(globalPropAVProList, i, propMap.avProCheck);
            _ResolvePropertyId(globalPropInvertList, i, propMap.invertY);
            _ResolvePropertyId(globalPropGammaList, i, propMap.applyGamma);
            _ResolvePropertyId(globalPropFitList, i, propMap.screenFit);
            _ResolvePropertyId(globalPropAspectRatioList, i, propMap.aspectRatio);
        }

        void _ResolvePropertyId(int[] idArray, int i, string name)
        {
            if (string.IsNullOrEmpty(name))
                return;

            if (!name.StartsWith("_Udon"))
                name = $"_Udon{name}";

            idArray[i] = VRCShader.PropertyToID(name);
        }

        void _LoadRealtimeEmissiveGammaProps(int i)
        {
            shaderPropMainTexList[i] = "_MainTex";
            shaderPropAVProList[i] = "_IsAVProInput";
            shaderPropInvertList[i] = "_InvertAVPro";
            shaderPropGammaList[i] = "_ApplyGammaAVPro";
            shaderPropFitList[i] = "_FitMode";
            shaderPropAspectRatioList[i] = "_TexAspectRatio";
        }

        void _LoadRenderOutProps(int i)
        {
            shaderPropMainTexList[i] = "_MainTex";
            shaderPropAVProList[i] = "";
            shaderPropInvertList[i] = "_FlipY";
            shaderPropGammaList[i] = "_ApplyGamma";
            shaderPropFitList[i] = "_FitMode";
            shaderPropAspectRatioList[i] = "_TexAspectRatio";
        }

        void _RestoreMaterialOverrides()
        {
            if (!useMaterialOverrides)
                return;

            for (int i = 0; i < screenMesh.Length; i++)
            {
                int index = screenMaterialIndex[i];
                if (index < 0 || !Utilities.IsValid(screenMesh[i]))
                    continue;

                Material[] materials = screenMesh[i].sharedMaterials;
                materials[index] = _originalScreenMaterial[i];
                screenMesh[i].sharedMaterials = materials;
            }
        }

        void _RestoreTextureOverrides()
        {
            if (!useTextureOverrides)
                return;

            for (int i = 0; i < materialUpdateList.Length; i++)
            {
                Material mat = materialUpdateList[i];
                string name = shaderPropMainTexList[baseIndexMat + i];
                if (mat == null || name == null || name.Length == 0)
                    continue;

                mat.SetTexture(name, _originalMaterialTexture[i]);
                //string avProProp = materialAVPropertyList[i];
                //if (avProProp != null && avProProp.Length > 0)
                //    mat.SetInt(avProProp, 0);

                _SetMatIntProperty(mat, shaderPropAVProList[baseIndexMat + i], _originalMatAVPro[i]);
                _SetMatIntProperty(mat, shaderPropInvertList[baseIndexMat + i], _originalMatInvert[i]);
                _SetMatIntProperty(mat, shaderPropGammaList[baseIndexMat + i], _originalMatGamma[i]);
                _SetMatIntProperty(mat, shaderPropFitList[baseIndexMat + i], _originalMatFit[i]);
                _SetMatFloatProperty(mat, shaderPropAspectRatioList[baseIndexMat + i], _originalMatAspectRatio[i]);
            }
        }

        public void _InternalOnVideoStateUpdate()
        {
            _DebugLowLevel("Event OnVideoStateUpdate");

            if (!videoPlayer)
            {
                _inError = false;
                _UpdateScreenMaterial(SCREEN_MODE_LOGO);
                return;
            }

            switch (videoPlayer.playerState)
            {
                case TXLVideoPlayer.VIDEO_STATE_STOPPED:
                    _inError = false;
                    _UpdateScreenMaterial(SCREEN_MODE_LOGO);
                    break;
                case TXLVideoPlayer.VIDEO_STATE_LOADING:
                    _UpdateScreenMaterial(SCREEN_MODE_LOADING);
                    break;
                case TXLVideoPlayer.VIDEO_STATE_PLAYING:
                    _inError = false;
                    _UpdateScreenMaterial(videoPlayer.syncing ? SCREEN_MODE_SYNC : SCREEN_MODE_NORMAL);
                    break;
                case TXLVideoPlayer.VIDEO_STATE_ERROR:
                    _inError = true;
                    _lastErrorCode = videoPlayer.lastErrorCode;
                    _UpdateScreenMaterial(SCREEN_MODE_ERROR);
                    break;
            }
        }

        public void _InternalOnSourceChanged()
        {
            _DebugLowLevel("Event OnSourceChanged");

            if (videoPlayer && videoPlayer.VideoManager)
            {
                captureRenderer = videoPlayer.VideoManager.CaptureRenderer;
                _screenSource = videoPlayer.VideoManager.ActiveSourceType;
            }

            _ResetCheckScreenMaterial();
        }

        int _CalculateScreenIndex(bool captureValid)
        {
            int mode = _screenMode;

            if (latchErrorState && _inError)
                mode = SCREEN_MODE_ERROR;

            switch (mode)
            {
                case SCREEN_MODE_UNINITIALIZED: return SCREEN_INDEX_LOGO;
                case SCREEN_MODE_LOGO: return SCREEN_INDEX_LOGO;
                case SCREEN_MODE_LOADING: return SCREEN_INDEX_LOADING;
                case SCREEN_MODE_SYNC: return SCREEN_INDEX_SYNC;
                case SCREEN_MODE_ERROR:
                    if (_lastErrorCode == VideoError.AccessDenied)
                        return SCREEN_INDEX_ERROR_BLOCKED;
                    else if (_lastErrorCode == VideoError.InvalidURL)
                        return SCREEN_INDEX_ERROR_INVALID;
                    else if (_lastErrorCode == VideoError.RateLimited)
                        return SCREEN_INDEX_ERROR_RATE;
                    return SCREEN_INDEX_ERROR;
                case SCREEN_MODE_NORMAL:
                default:
                    if (!currentValid)
                    {
                        if (loadingMaterial != null && _checkFrameCount < 50)
                            return SCREEN_INDEX_LOADING;
                        else if (audioMaterial != null && _checkFrameCount >= 50)
                            return SCREEN_INDEX_AUDIO;
                        else if (logoMaterial != null)
                            return SCREEN_INDEX_LOGO;
                    }
                    return SCREEN_INDEX_PLAYBACK;
            }
        }

        void _UpdateScreenMaterial(int screenMode)
        {
            _DebugLowLevel($"Update screen material: mode={screenMode}");

            int fit = _VideoScreenFit();
            if (_screenMode == screenMode && _screenFit == fit)
                return;

            _screenMode = screenMode;
            _screenFit = fit;

            _ResetCheckScreenMaterial();
        }

        public Texture _GetTextureOverride(ScreenOverrideType overrideType)
        {
            return replacementTextures[(int)overrideType];
        }

        [Obsolete("Use version of method that takes ScreenOverrideType")]
        public Texture _GetTextureOverride(int screenIndex)
        {
            return replacementTextures[screenIndex];
        }

        public void _SetTextureOverride(ScreenOverrideType overrideType, Texture texture)
        {
            int screenIndex = (int)overrideType;
            if (replacementTextures[screenIndex] != texture)
            {
                replacementTextures[screenIndex] = texture;
                _ResetCheckScreenMaterial();
            }
        }

        [Obsolete("Use version of method that takes ScreenOverrideType")]
        public void _SetTextureOverride(int screenIndex, Texture texture)
        {
            if (replacementTextures[screenIndex] != texture)
            {
                replacementTextures[screenIndex] = texture;
                _ResetCheckScreenMaterial();
            }
        }

        public Texture _GetResolvedTextureOverride(ScreenOverrideType overrideType)
        {
            int resIndex = (int)overrideType;
            Texture tex = replacementTextures[resIndex];

            while (tex == null && screenIndexFallbackMap[resIndex] >= 0)
            {
                resIndex = screenIndexFallbackMap[resIndex];
                tex = replacementTextures[resIndex];
            }

            return tex;
        }

        [Obsolete("Use version of method that takes ScreenOverrideType")]
        public Texture _GetResolvedTextureOverride(int screenIndex)
        {
            return _GetResolvedTextureOverride((ScreenOverrideType)screenIndex);
        }

        public void _Refresh()
        {
            _ResetCheckScreenMaterial();
        }

        void _ResetCheckScreenMaterial()
        {
            _EnsureInit();

            _checkFrameCount = 0;

            _ResetCaptureData();
            currentValid = false;

            captureTexture = CaptureValid();
            currentValid = Utilities.IsValid(captureTexture);

            int screenIndex = _CalculateScreenIndex(currentValid);

            if (useMaterialOverrides)
                _UpdateObjects(replacementMaterials[screenIndex]);

            if (useTextureOverrides)
            {
                Texture replacement = _GetResolvedTextureOverride((ScreenOverrideType)screenIndex);
                _UpdateCaptureData(replacement, captureTexture);
                _UpdateMaterials();
                _UpdatePropertyBlocks();
                _UpdateGlobalProperties();
            }

            if (!currentValid)
                _QueueUpdateCheckFrames(1);
            else
            {
                _DebugLog("Capture valid");
                _UpdateHandlers(EVENT_CAPTURE_VALID);
                _QueueUpdateCheckIfNoPending(2);
            }
        }

        void _QueueUpdateCheck(float delay)
        {
            pendingUpdates += 1;
            SendCustomEventDelayedSeconds(nameof(_InternalCheckUpdateScreenMaterial), delay);
        }

        void _QueueUpdateCheckIfNoPending(float delay)
        {
            if (pendingUpdates > 0)
                return;
            _QueueUpdateCheck(delay);
        }

        void _QueueUpdateCheckFrames(int frames)
        {
            pendingUpdates += 1;
            SendCustomEventDelayedFrames(nameof(_InternalCheckUpdateScreenMaterial), frames);
        }

        public void _InternalCheckUpdateScreenMaterial()
        {
            pendingUpdates -= 1;

            if (_screenMode != SCREEN_MODE_NORMAL)
            {
                _QueueUpdateCheckIfNoPending(2);
                return;
            }

            _EnsureInit();

            captureTexture = CaptureValid();
            bool prevValid = currentValid;
            currentValid = Utilities.IsValid(captureTexture);
            _DebugLowLevel($"Check Update Screen Material: valid={currentValid}");

            int screenIndex = _CalculateScreenIndex(currentValid);

            if (useMaterialOverrides)
                _UpdateObjects(replacementMaterials[screenIndex]);

            if (useTextureOverrides)
            {
                Texture replacement = _GetResolvedTextureOverride((ScreenOverrideType)screenIndex);
                _UpdateCaptureData(replacement, captureTexture);
                _UpdateMaterials();
                _UpdatePropertyBlocks();
                _UpdateGlobalProperties();
            }

            if (!currentValid)
            {
                _checkFrameCount += 1;
                _QueueUpdateCheckFrames(_checkFrameCount < 100 ? 1 : 10);
            }
            else
            {
                if (!prevValid)
                {
                    _DebugLog("Capture valid");
                    _UpdateHandlers(EVENT_CAPTURE_VALID);
                }
                _QueueUpdateCheckIfNoPending(2);
            }
        }

        int _VideoScreenFit()
        {
            return videoPlayer ? videoPlayer.screenFit : TXLVideoPlayer.SCREEN_FIT;
        }

        bool _IsQuest()
        {
            return videoPlayer ? videoPlayer.IsQuest : false;
        }

        void _ResetCaptureData()
        {
            currentTexture = null;
            currentAVPro = false;
            currentInvert = false;
            currentGamma = false;
            currentFit = _VideoScreenFit();
            currentAspectRatio = 0;
        }

        void _UpdateCaptureData(Texture replacementTex, Texture captureTex)
        {
            Texture lastTex = currentTexture;

            _ResetCaptureData();

            currentAspectRatio = (overrideAspectRatio && replacementTex) ? aspectRatio : 0;

            if (Utilities.IsValid(replacementTex))
            {
                currentTexture = replacementTex;
            }
            else
            {
                if (_screenSource == VideoSource.VIDEO_SOURCE_AVPRO && captureRenderer)
                {
                    currentTexture = captureTex;
                    currentAVPro = true;
                }
                else if (_screenSource == VideoSource.VIDEO_SOURCE_UNITY && captureRenderer)
                    currentTexture = captureTex;

                if (currentAVPro)
                {
                    currentInvert = !_IsQuest();
                    currentGamma = true;
                }

                if (currentTexture)
                {
                    currentAspectRatio = (float)currentTexture.width / currentTexture.height;
                    currentRes = new Vector2Int(currentTexture.width, currentTexture.height);

                    for (int i = 0; i < renderOutCrt.Length; i++)
                    {
                        if (!renderOutResize[i])
                            continue;

                        CustomRenderTexture crt = renderOutCrt[i];
                        if (!crt)
                            continue;

                        if (crt.width == currentTexture.width && crt.height == currentTexture.height)
                            continue;

                        float aspect = renderOutTargetAspect[i];
                        int newWidth = currentTexture.width;
                        int newHeight = currentTexture.height;

                        if (renderOutExpandSize[i] && aspect > 0)
                        {
                            if (aspect > currentAspectRatio)
                                newWidth = (int)Math.Round(newHeight * aspect);
                            else
                                newHeight = (int)Math.Round(newWidth / aspect);

                            if (newWidth > 4096 || newHeight > 4096)
                            {
                                newWidth = currentTexture.width;
                                newHeight = currentTexture.height;
                            }
                        }

                        crt.Release();
                        crt.width = newWidth;
                        crt.height = newHeight;
                        crt.Create();
                    }
                }
            }

            if (lastTex != currentTexture)
                _UpdateHandlers(EVENT_UPDATE);
        }

        void _UpdateObjects(Material replacementMat)
        {
            for (int i = 0; i < screenMesh.Length; i++)
            {
                int index = screenMaterialIndex[i];
                if (index < 0)
                    continue;

                Material newMat = replacementMat;
                if (newMat == null)
                    newMat = _GetPlaybackMaterial(i);
                if (newMat == null && logoMaterial != null)
                    newMat = logoMaterial;

                if (newMat != null)
                {
                    Material[] materials = screenMesh[i].sharedMaterials;
                    materials[index] = newMat;
                    screenMesh[i].sharedMaterials = materials;
                }
            }
        }

        private Texture ValidCurrentTexture
        {
            get { return currentTexture ? currentTexture : Texture2D.blackTexture; }
        }

        void _UpdateMaterials()
        {
            Texture validCurrent = ValidCurrentTexture;

            int fit = _VideoScreenFit();
            for (int i = 0; i < materialUpdateList.Length; i++)
            {
                Material mat = materialUpdateList[i];
                string name = shaderPropMainTexList[baseIndexMat + i];
                if (mat == null || name == null || name.Length == 0)
                    continue;

                mat.SetTexture(name, validCurrent);

                _SetMatIntProperty(mat, shaderPropAVProList[baseIndexMat + i], currentAVPro ? 1 : 0);
                _SetMatIntProperty(mat, shaderPropInvertList[baseIndexMat + i], currentInvert ? 1 : 0);
                _SetMatIntProperty(mat, shaderPropGammaList[baseIndexMat + i], currentGamma ? 1 : 0);
                _SetMatIntProperty(mat, shaderPropFitList[baseIndexMat + i], fit);
                _SetMatFloatProperty(mat, shaderPropAspectRatioList[baseIndexMat + i], currentAspectRatio);
            }

            for (int i = 0; i < renderOutCrt.Length; i++)
            {
                CustomRenderTexture crt = renderOutCrt[i];
                if (!crt)
                    continue;

                Material mat = crt.material;
                if (Utilities.IsValid(mat))
                {
                    mat.SetTexture(shaderPropMainTexList[baseIndexCrt], validCurrent);

                    _SetMatIntProperty(mat, shaderPropAVProList[baseIndexCrt], currentAVPro ? 1 : 0);
                    _SetMatIntProperty(mat, shaderPropGammaList[baseIndexCrt], currentGamma ? 1 : 0);
                    _SetMatIntProperty(mat, shaderPropInvertList[baseIndexCrt], currentInvert ? 1 : 0);
                    _SetMatIntProperty(mat, shaderPropFitList[baseIndexCrt], fit);
                    _SetMatFloatProperty(mat, shaderPropAspectRatioList[baseIndexCrt], currentAspectRatio);

                    bool isRT = validCurrent.GetType() == typeof(RenderTexture) || validCurrent.GetType() == typeof(CustomRenderTexture);
                    if (_screenMode == SCREEN_MODE_NORMAL || isRT)
                        crt.updateMode = CustomRenderTextureUpdateMode.Realtime;
                    else
                    {
                        crt.updateMode = CustomRenderTextureUpdateMode.OnDemand;
                        crt.Update(crt.doubleBuffered ? 2 : 1);
                    }
                }
            }
        }

        void _UpdatePropertyBlocks()
        {
            Texture validCurrent = ValidCurrentTexture;

            int fit = _VideoScreenFit();
            for (int i = 0; i < propMeshList.Length; i++)
            {
                MeshRenderer renderer = propMeshList[i];
                string texName = shaderPropMainTexList[baseIndexProp + i];
                if (renderer == null || name == null || name.Length == 0)
                    continue;

                bool useMatIndex = propMaterialOverrideList[i] == 1;
                if (useMatIndex)
                    renderer.GetPropertyBlock(block, propMaterialIndexList[i]);
                else
                    renderer.GetPropertyBlock(block);

                block.SetTexture(texName, validCurrent);

                _SetIntProperty(shaderPropAVProList[baseIndexProp + i], currentAVPro ? 1 : 0);
                _SetIntProperty(shaderPropGammaList[baseIndexProp + i], currentGamma ? 1 : 0);
                _SetIntProperty(shaderPropInvertList[baseIndexProp + i], currentInvert ? 1 : 0);
                _SetIntProperty(shaderPropFitList[baseIndexProp + i], fit);
                _SetFloatProperty(shaderPropAspectRatioList[baseIndexProp + i], currentAspectRatio);

                if (useMatIndex)
                    renderer.SetPropertyBlock(block, propMaterialIndexList[i]);
                else
                    renderer.SetPropertyBlock(block);
            }
        }

        void _UpdateGlobalProperties()
        {
            Texture validCurrent = ValidCurrentTexture;

            int fit = _VideoScreenFit();
            for (int i = 0; i < globalPropMainTexList.Length; i++)
            {
                if (globalPropMainTexList[i] != 0)
                    VRCShader.SetGlobalTexture(globalPropMainTexList[i], validCurrent);
                if (globalPropAVProList[i] != 0)
                    VRCShader.SetGlobalInteger(globalPropAVProList[i], currentAVPro ? 1 : 0);
                if (globalPropGammaList[i] != 0)
                    VRCShader.SetGlobalInteger(globalPropGammaList[i], currentGamma ? 1 : 0);
                if (globalPropInvertList[i] != 0)
                    VRCShader.SetGlobalInteger(globalPropInvertList[i], currentInvert ? 1 : 0);
                if (globalPropFitList[i] != 0)
                    VRCShader.SetGlobalInteger(globalPropFitList[i], fit);
                if (globalPropAspectRatioList[i] != 0)
                    VRCShader.SetGlobalFloat(globalPropAspectRatioList[i], currentAspectRatio);
            }
        }

        void _SetIntProperty(string prop, int value)
        {
            if (prop != null && prop.Length > 0)
                block.SetInt(prop, value);
        }

        void _SetFloatProperty(string prop, float value)
        {
            if (prop != null && prop.Length > 0)
                block.SetFloat(prop, value);
        }

        void _SetMatIntProperty(Material mat, string prop, int value)
        {
            if (prop != null && prop.Length > 0)
                mat.SetInt(prop, value);
        }

        void _SetMatFloatProperty(Material mat, string prop, float value)
        {
            if (prop != null && prop.Length > 0)
                mat.SetFloat(prop, value);
        }

        int _GetMatIntProperty(Material mat, string prop)
        {
            if (prop != null && prop.Length > 0)
                return mat.GetInt(prop);
            return 0;
        }

        float _GetMatFloatProperty(Material mat, string prop)
        {
            if (prop != null && prop.Length > 0)
                return mat.GetFloat(prop);
            return 0;
        }

        Material _GetPlaybackMaterial(int meshIndex)
        {
            return Utilities.IsValid(playbackMaterial) ? playbackMaterial : _originalScreenMaterial[meshIndex];
        }

        Texture CaptureValid()
        {
            if (!Utilities.IsValid(captureRenderer))
            {
                _DebugLowLevel("No valid capture renderer");
                return null;
            }

            if (_screenSource == VideoSource.VIDEO_SOURCE_AVPRO)
            {
                Material mat = captureRenderer.sharedMaterial;
                if (mat == null)
                    return null;

                Texture tex = mat.GetTexture("_MainTex");
                if (tex == null)
                    return null;
                if (tex.width < 16 || tex.height < 16)
                    return null;

                if (!currentValid)
                    _DebugLog($"Resolution {tex.width} x {tex.height}");
                return tex;
            }

            if (_screenSource == VideoSource.VIDEO_SOURCE_UNITY)
            {
                captureRenderer.GetPropertyBlock(block);
                Texture tex = block.GetTexture("_MainTex");
                if (tex == null)
                    return null;
                if (tex.width < 16 || tex.height < 16)
                    return null;

                if (!currentValid)
                    _DebugLog($"Resolution {tex.width} x {tex.height}");
                return tex;
            }

            _DebugLowLevel("No valid screen source selected");

            return null;
        }

        void _DebugLog(string message)
        {
            if (vrcLogging)
                Debug.Log("[VideoTXL:ScreenManager] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("ScreenManager", message);
        }

        void _DebugError(string message, bool force = false)
        {
            if (vrcLogging || force)
                Debug.LogError("[VideoTXL:ScreenManager] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("ScreenManager", message);
        }

        void _DebugLowLevel(string message)
        {
            if (lowLevelLogging)
                _DebugLog(message);
        }

        public void _SetDebugState(DebugState debug)
        {
            if (debugState)
            {
                debugState._Unregister(DebugState.EVENT_UPDATE, this, nameof(_InternalUpdateDebugState));
                debugState = null;
            }

            if (!debug)
                return;

            debugState = debug;
            debugState._Register(DebugState.EVENT_UPDATE, this, nameof(_InternalUpdateDebugState));
            debugState._SetContext(this, nameof(_InternalUpdateDebugState), "ScreenManager");
        }

        public void _InternalUpdateDebugState()
        {
            VRCPlayerApi owner = Networking.GetOwner(gameObject);
            debugState._SetValue("videoPlayer", videoPlayer ? videoPlayer.ToString() : "--");
            debugState._SetValue("isQuest", _IsQuest().ToString());
            debugState._SetValue("captureRenderer", captureRenderer ? captureRenderer.ToString() : "--");
            debugState._SetValue("screenSource", _screenSource.ToString());
            debugState._SetValue("screenMode", _screenMode.ToString());
            debugState._SetValue("screenFit", _screenFit.ToString());
            debugState._SetValue("inError", _inError.ToString());
            debugState._SetValue("lastErrorCode", _lastErrorCode.ToString());
            debugState._SetValue("checkFrameCount", _checkFrameCount.ToString());
            debugState._SetValue("pendingUpdates", pendingUpdates.ToString());
            debugState._SetValue("captureTexture", captureTexture ? captureTexture.ToString() : "--");
            debugState._SetValue("currentTexture", currentTexture ? currentTexture.ToString() : "--");
            debugState._SetValue("currentAVPro", currentAVPro.ToString());
            debugState._SetValue("currentGamma", currentGamma.ToString());
            debugState._SetValue("currentInvert", currentInvert.ToString());
            debugState._SetValue("currentFit", currentFit.ToString());
            debugState._SetValue("currentAspectRatio", currentAspectRatio.ToString());
            debugState._SetValue("currentResolution", $"{currentRes.x}x{currentRes.y}");
            debugState._SetValue("currentValid", currentValid.ToString());
        }
    }
}
