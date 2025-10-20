﻿
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

    public enum VRSLMode
    {
        Infer = -1,
        Horizontal = 0,
        Vertical = 1,
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

        int meshCount = 0;
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

        [Tooltip("Regularly check the underlying capture source for changes.  This is necessary to handle media sources that may change their resolution or format mid-playback.")]
        [SerializeField] internal bool monitorCaptureSource = true;
        [Tooltip("The number of seconds between each capture source check.")]
        [SerializeField] internal float monitorCaptureSourceInterval = 2f;

        int materialCount = 0;
        [SerializeField] internal Material[] materialUpdateList;
        [SerializeField] internal ScreenPropertyMap[] materialPropertyList;

        int propBlockCount = 0;
        [SerializeField] internal MeshRenderer[] propMeshList;
        [SerializeField] internal int[] propMaterialOverrideList;
        [SerializeField] internal int[] propMaterialIndexList;
        [SerializeField] internal ScreenPropertyMap[] propPropertyList;

        [SerializeField] internal ScreenPropertyMap[] globalPropertyList;

        [SerializeField] internal bool useRenderOut = false;
        [SerializeField] internal CustomRenderTexture outputCRT;
        [SerializeField] internal ScreenPropertyMap outputMaterialProperties;

        int crtCount = 0;
        [SerializeField] internal CustomRenderTexture[] renderOutCrt;
        [SerializeField] internal ScreenPropertyMap[] renderOutMatProps;
        [SerializeField] internal Vector2Int[] renderOutSize;
        [SerializeField] internal float[] renderOutTargetAspect;
        [SerializeField] internal bool[] renderOutResize;
        [SerializeField] internal bool[] renderOutExpandSize;
        [SerializeField] internal bool[] renderOutGlobalTex;
        [SerializeField] internal bool[] renderOutDoubleBufferAVPro;
        [SerializeField] internal bool[] renderOutDoubleBufferUnity;

        [SerializeField] internal bool downloadLogoImage;
        [SerializeField] internal VRCUrl downloadLogoImageUrl;
        [SerializeField] internal ImageDownloadManager imageDownloadManager;

        // VRSL Integration
        [SerializeField] internal bool vrslEnabled;
        [SerializeField] internal UdonBehaviour vrslController;
        [SerializeField] internal RenderTexture vrslDmxRT;
        [SerializeField] internal Vector3 vrslOffsetScale;
        [SerializeField] internal float vrslSourceAspectRatio;
        [SerializeField] internal bool vrslDoubleBufferAVPro = true;
        [SerializeField] internal bool vrslDoubleBufferUnity = false;
        [SerializeField] internal Material vrslBlitMat;
        [SerializeField] internal VRSLMode vrslMode = VRSLMode.Infer;

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
        string[] shaderPropDoubleBufferedList;

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
        int imageDownloadClaim;

        [Obsolete("Use EVENT_TEX_CHANGED")]
        public const int EVENT_UPDATE = 0;
        public const int EVENT_TEX_CHANGED = 0;
        public const int EVENT_CAPTURE_VALID = 1;
        public const int EVENT_CAPTURE_INVALID = 2;
        public const int EVENT_CAPTURE_TEX_CHANGED = 3;
        public const int EVENT_CAPTURE_RES_CHANGED = 4;
        public const int EVENT_RES_CHANGED = 5;
        const int EVENT_COUNT = 6;

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
            _InitVRSL();

#if COMPILER_UDONSHARP
            _SetupMeshMaterials();
            _InitTextureOverrides();
#endif

            _InitCRTDoubleBuffer();

            if (downloadLogoImage)
            {
                if (imageDownloadManager != null)
                    imageDownloadClaim = imageDownloadManager._RequestImage(downloadLogoImageUrl, this, nameof(_InternalOnImageDispatch));
                else
                {
                    imageDownloader = new VRCImageDownloader();

                    TextureInfo info = new TextureInfo();
                    info.GenerateMipMaps = true;

                    imageDownloader.DownloadImage(downloadLogoImageUrl, null, (IUdonEventReceiver)this, info);
                }
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
            _EnsureInit();
            _UnregisterVideoPlayerListeners();

            captureRenderer = null;
            _screenSource = VideoSource.VIDEO_SOURCE_NONE;

            this.videoPlayer = videoPlayer;
            _RegisterVideoPlayerListeners();
        }

        void _RegisterVideoPlayerListeners()
        {
            if (videoPlayer)
            {
                videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_STATE_UPDATE, this, nameof(_InternalOnVideoStateUpdate));
                videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_SOURCE_CHANGE, this, nameof(_InternalOnSourceChanged));
            }

            _InternalOnSourceChanged();
            _InternalOnVideoStateUpdate();
        }

        void _UnregisterVideoPlayerListeners()
        {
            if (videoPlayer)
            {
                videoPlayer._Unregister(TXLVideoPlayer.EVENT_VIDEO_STATE_UPDATE, this, nameof(_InternalOnVideoStateUpdate));
                videoPlayer._Unregister(TXLVideoPlayer.EVENT_VIDEO_SOURCE_CHANGE, this, nameof(_InternalOnSourceChanged));
            }
        }

        public void _InternalOnImageDispatch()
        {
            Texture2D image = imageDownloadManager.CurrentImage;
            if (image != null)
            {
                _SetTextureOverride(ScreenOverrideType.Logo, image);
                _ResetCheckScreenMaterial();
            }
        }

        public override void OnImageLoadSuccess(IVRCImageDownload result)
        {
            _SetTextureOverride(ScreenOverrideType.Logo, result.Result);
            _ResetCheckScreenMaterial();
        }

        private void OnEnable()
        {
            if (Initialized)
                _RegisterVideoPlayerListeners();
        }

        private void OnDisable()
        {
            _UnregisterVideoPlayerListeners();

#if COMPILER_UDONSHARP
            _RestoreMeshMaterialOverrides();
            _RestoreSharedMaterialOverrides();
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

        // The current texture, which could be one of the placeholders
        public Texture CurrentTexture
        {
            get { return currentTexture; }
        }

        // The capture texture, which is the raw texture from AVPro or Unity, or null if determined invalid
        public Texture CaptureTexture
        {
            get { return validatedTexture; }
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

        public bool CurrentTextureIsError
        {
            get 
            {
                _EnsureInit();
                return currentTexture == replacementTextures[(int)ScreenOverrideType.Error]
                    || currentTexture == replacementTextures[(int)ScreenOverrideType.ErrorRate]
                    || currentTexture == replacementTextures[(int)ScreenOverrideType.ErrorInvalid]
                    || currentTexture == replacementTextures[(int)ScreenOverrideType.ErrorBlocked];
            }
        }

        public bool _GetVRSLDoubleBuffered(VideoSourceBackend type)
        {
            if (type == VideoSourceBackend.Unity)
                return vrslDoubleBufferUnity;
            if (type == VideoSourceBackend.AVPro)
                return vrslDoubleBufferAVPro;

            return false;
        }

        public void _SetVRSLDoubleBuffered(VideoSourceBackend type, bool state)
        {
            if (type == VideoSourceBackend.Unity)
                vrslDoubleBufferUnity = state;
            else if (type == VideoSourceBackend.AVPro)
                vrslDoubleBufferAVPro = state;

            _UpdateVRSLBuffer();
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
                _LegacyCRTUpgrade();
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
            shaderPropDoubleBufferedList = new string[totalPropLength];

            // Material Props
            _SetupSharedMaterials();

            // Property Block Props
            _SetupPropertyBlocks();

            // Output Material
            _SetupCRTs();

            // Global Shader Props
            _SetupGlobalProperties();

            // Capture original material textures
            _CaptureSharedMaterialOverrides();
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
            shaderPropDoubleBufferedList[i] = propMap.doubleBuffered;
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
            shaderPropDoubleBufferedList[i] = "";
        }

        void _LoadRenderOutProps(int i)
        {
            shaderPropMainTexList[i] = "_MainTex";
            shaderPropAVProList[i] = "";
            shaderPropInvertList[i] = "_FlipY";
            shaderPropGammaList[i] = "_ApplyGamma";
            shaderPropFitList[i] = "_FitMode";
            shaderPropAspectRatioList[i] = "_TexAspectRatio";
            shaderPropDoubleBufferedList[i] = "_DoubleBuffered";
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

                _UpdateCRTDoubleBuffer();
                _UpdateVRSLBuffer();
            }

            _ResetCheckScreenMaterial();
        }

        int _ScreenIndexByMode(int mode)
        {
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

        int _CalculateScreenIndex(bool captureValid)
        {
            int mode = _screenMode;

            if (latchErrorState && _inError)
                mode = SCREEN_MODE_ERROR;

            int index = _ScreenIndexByMode(mode);

            if (index == SCREEN_INDEX_PLAYBACK || index == SCREEN_INDEX_AUDIO)
            {
                if (videoPlayer && videoPlayer.currentUrlSource && videoPlayer.currentUrlSource.DisplayOverride == VideoDisplayOverride.Logo)
                    index = SCREEN_INDEX_LOGO;
            }

            return index;
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
            _EnsureInit();

            return replacementTextures[(int)overrideType];
        }

        [Obsolete("Use version of method that takes ScreenOverrideType")]
        public Texture _GetTextureOverride(int screenIndex)
        {
            _EnsureInit();

            return replacementTextures[screenIndex];
        }

        public void _SetTextureOverride(ScreenOverrideType overrideType, Texture texture)
        {
            _EnsureInit();

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
            _EnsureInit();

            if (replacementTextures[screenIndex] != texture)
            {
                replacementTextures[screenIndex] = texture;
                _ResetCheckScreenMaterial();
            }
        }

        public Texture _GetResolvedTextureOverride(ScreenOverrideType overrideType)
        {
            _EnsureInit();

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

            bool textureChanged = _ValidateCapture();
            currentValid = Utilities.IsValid(validatedTexture);

            int screenIndex = _CalculateScreenIndex(currentValid);

            if (useMaterialOverrides)
                _UpdateObjects(replacementMaterials[screenIndex]);

            if (useTextureOverrides)
            {
                Texture replacement = _GetResolvedTextureOverride((ScreenOverrideType)screenIndex);
                _UpdateCaptureData(replacement, validatedTexture);
                _UpdateMaterials();
                _UpdatePropertyBlocks();
                _UpdateGlobalProperties();
            }

            if (!currentValid)
            {
                _UpdateHandlers(EVENT_CAPTURE_INVALID);
                _QueueUpdateCheckFrames(1);
            }
            else
            {
                _DebugLog("Capture valid");
                _UpdateHandlers(EVENT_CAPTURE_VALID);

                if (monitorCaptureSource)
                    _QueueUpdateCheckIfNoPending(monitorCaptureSourceInterval);
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

        Texture lastReplacement;
        public void _InternalCheckUpdateScreenMaterial()
        {
            pendingUpdates -= 1;
            if (!gameObject.activeInHierarchy)
                return;

            if (_screenMode != SCREEN_MODE_NORMAL)
            {
                _QueueUpdateCheckIfNoPending(2);
                return;
            }

            _EnsureInit();

            bool textureChanged = _ValidateCapture();
            bool prevValid = currentValid;
            currentValid = Utilities.IsValid(validatedTexture);
            _DebugLowLevel($"Check Update Screen Material: valid={currentValid}");

            int screenIndex = _CalculateScreenIndex(currentValid);

            if (useMaterialOverrides)
                _UpdateObjects(replacementMaterials[screenIndex]);

            if (useTextureOverrides)
            {
                Texture replacement = _GetResolvedTextureOverride((ScreenOverrideType)screenIndex);
                if (textureChanged || replacement != lastReplacement)
                {
                    lastReplacement = replacement;

                    _UpdateCaptureData(replacement, validatedTexture);
                    _UpdateMaterials();
                    _UpdatePropertyBlocks();
                    _UpdateGlobalProperties();
                }
            }

            if (!currentValid)
            {
                if (prevValid)
                    _UpdateHandlers(EVENT_CAPTURE_INVALID);

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
                if (monitorCaptureSource)
                    _QueueUpdateCheckIfNoPending(monitorCaptureSourceInterval);
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
                currentGamma = !currentTexture.isDataSRGB;

                if (lastTex && lastTex != replacementTex)
                {
                    if (lastTex.width != replacementTex.width || lastTex.height != replacementTex.height)
                        _UpdateHandlers(EVENT_RES_CHANGED);
                } else if (!lastTex)
                    _UpdateHandlers(EVENT_RES_CHANGED);
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

                _UpdateCRTCaptureData();

                if (lastTex || captureTex)
                {
                    if (lastTex && captureTex && lastTex != captureTex)
                    {
                        if (lastTex.width != replacementTex.width || lastTex.height != replacementTex.height)
                            _UpdateHandlers(EVENT_RES_CHANGED);
                    }
                    else
                        _UpdateHandlers(EVENT_RES_CHANGED);
                }
            }

            if (lastTex != currentTexture)
                _UpdateHandlers(EVENT_TEX_CHANGED);
        }

        

        private Texture ValidCurrentTexture
        {
            get { return currentTexture ? currentTexture : Texture2D.blackTexture; }
        }

        private void Update()
        {
            _BlitVRSL();
        }

        void _UpdateMaterials()
        {
            _UpdateSharedMaterials();
            _UpdateCRTs();

            _UpdateVRSL();
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

        Texture validatedTexture = null;
        int validatedW = 0;
        int validatedH = 0;

        void _ResetValidatedTexture()
        {
            validatedTexture = null;
            validatedW = 0;
            validatedH = 0;
        }

        bool _UpdateValidatedTexture(Texture texture)
        {
            if (texture == validatedTexture)
            {
                if (texture)
                {
                    int w = texture.width;
                    int h = texture.height;
                    if (validatedW - w != 0 || validatedH - h != 0)
                    {
                        validatedW = w;
                        validatedH = h;

                        _UpdateHandlers(EVENT_CAPTURE_RES_CHANGED);
                        return true;
                    }
                }
                return false;
            }

            validatedTexture = texture;
            if (texture)
            {
                int w = texture.width;
                int h = texture.height;
                if (validatedW - w != 0 || validatedH - h != 0)
                {
                    validatedW = w;
                    validatedH = h;

                    _UpdateHandlers(EVENT_CAPTURE_RES_CHANGED);
                }
            } else
            {
                validatedW = 0;
                validatedH = 0;

                _UpdateHandlers(EVENT_CAPTURE_RES_CHANGED);
            }

            _UpdateHandlers(EVENT_CAPTURE_TEX_CHANGED);
            return true;
        }

        bool _ValidateCapture()
        {
            if (!Utilities.IsValid(captureRenderer))
            {
                _DebugLowLevel("No valid capture renderer");
                return false;
            }

            if (_screenSource == VideoSource.VIDEO_SOURCE_AVPRO)
            {
                Material mat = captureRenderer.sharedMaterial;
                if (mat == null)
                    return _UpdateValidatedTexture(null);

                Texture tex = mat.GetTexture("_MainTex");
                if (tex == null)
                    return _UpdateValidatedTexture(null);
                if (tex.width < 16 || tex.height < 16)
                    return _UpdateValidatedTexture(null);

                if (!currentValid)
                    _DebugLog($"Resolution {tex.width} x {tex.height}");

                return _UpdateValidatedTexture(tex);
            }

            if (_screenSource == VideoSource.VIDEO_SOURCE_UNITY)
            {
                captureRenderer.GetPropertyBlock(block);
                Texture tex = block.GetTexture("_MainTex");
                if (tex == null)
                    return _UpdateValidatedTexture(null);
                if (tex.width < 16 || tex.height < 16)
                    return _UpdateValidatedTexture(null);

                if (!currentValid)
                    _DebugLog($"Resolution {tex.width} x {tex.height}");

                return _UpdateValidatedTexture(tex);
            }

            _DebugLowLevel("No valid screen source selected");

            return _UpdateValidatedTexture(null);
        }

        #region Mesh Materials

        Material[] originalScreenMaterial;
        Material[] activePlaybackMaterial;
        Material lastReplacementMat;

        void _SetupMeshMaterials()
        {
            useMaterialOverrides = true;

            if (!Utilities.IsValid(screenMesh) || screenMesh.Length == 0)
            {
                useMaterialOverrides = false;
                return;
            }

            // Capture original screen materials
            meshCount = screenMesh.Length;
            originalScreenMaterial = new Material[meshCount];
            activePlaybackMaterial = new Material[meshCount];

            for (int i = 0; i < meshCount; i++)
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

                originalScreenMaterial[i] = materials[index];
                activePlaybackMaterial[i] = playbackMaterial ? playbackMaterial : materials[index];
            }
        }

        void _RestoreMeshMaterialOverrides()
        {
            if (meshCount == 0)
                return;

            for (int i = 0; i < meshCount; i++)
            {
                int index = screenMaterialIndex[i];
                if (index < 0 || !Utilities.IsValid(screenMesh[i]))
                    continue;

                Material[] materials = screenMesh[i].sharedMaterials;
                materials[index] = originalScreenMaterial[i];
                screenMesh[i].sharedMaterials = materials;
            }
        }

        void _UpdateObjects(Material replacementMat)
        {
            if (replacementMat == lastReplacementMat)
                return;

            lastReplacementMat = replacementMat;

            for (int i = 0; i < meshCount; i++)
            {
                int index = screenMaterialIndex[i];
                if (index < 0)
                    continue;

                Material newMat = replacementMat;
                if (newMat == null)
                    newMat = activePlaybackMaterial[i];
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

        #endregion

        #region CRT

        void _SetupCRTs()
        {
            crtCount = renderOutCrt.Length;
            if (crtCount == 0)
                return;

            for (int i = 0; i < crtCount; i++)
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

        void _UpdateCRTs()
        {
            if (crtCount == 0)
                return;

            Texture validCurrent = ValidCurrentTexture;
            int fit = _VideoScreenFit();

            for (int i = 0; i < crtCount; i++)
            {
                CustomRenderTexture crt = renderOutCrt[i];
                if (!crt)
                    continue;

                Material mat = crt.material;
                if (Utilities.IsValid(mat))
                {
                    mat.SetTexture(shaderPropMainTexList[baseIndexCrt + i], validCurrent);

                    _SetMatIntProperty(mat, shaderPropAVProList[baseIndexCrt + i], currentAVPro ? 1 : 0);
                    _SetMatIntProperty(mat, shaderPropGammaList[baseIndexCrt + i], currentGamma ? 1 : 0);
                    _SetMatIntProperty(mat, shaderPropInvertList[baseIndexCrt + i], currentInvert ? 1 : 0);
                    _SetMatIntProperty(mat, shaderPropFitList[baseIndexCrt + i], fit);
                    _SetMatFloatProperty(mat, shaderPropAspectRatioList[baseIndexCrt + i], currentAspectRatio);

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

            SendCustomEventDelayedFrames(nameof(_CrtManualUpdate), 1);
        }

        void _UpdateCRTCaptureData()
        {
            if (crtCount == 0 || !currentTexture)
                return;

            currentAspectRatio = (float)currentTexture.width / currentTexture.height;
            currentRes = new Vector2Int(currentTexture.width, currentTexture.height);

            for (int i = 0; i <crtCount; i++)
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

        public void _CrtManualUpdate()
        {
            for (int i = 0; i < crtCount; i++)
            {
                CustomRenderTexture crt = renderOutCrt[i];
                if (!crt)
                    continue;

                if (crt.updateMode == CustomRenderTextureUpdateMode.OnDemand)
                    crt.Update();
            }
        }

        public bool _GetCRTDoubleBuffered(VideoSourceBackend type, int crtIndex)
        {
            if (crtIndex < 0 || crtIndex >= crtCount)
                return false;

            if (type == VideoSourceBackend.AVPro)
                return renderOutDoubleBufferAVPro[crtIndex];
            if (type == VideoSourceBackend.Unity)
                return renderOutDoubleBufferUnity[crtIndex];

            return false;
        }

        public void _SetCRTDoubleBuffered(VideoSourceBackend type, int crtIndex, bool state)
        {
            if (crtIndex < 0 || crtIndex >= crtCount)
                return;

            if (type == VideoSourceBackend.AVPro)
                renderOutDoubleBufferAVPro[crtIndex] = state;
            if (type == VideoSourceBackend.Unity)
                renderOutDoubleBufferUnity[crtIndex] = state;

            _UpdateCRTDoubleBuffer(crtIndex);
        }

        // Legacy Upgrade
        void _InitCRTDoubleBuffer()
        {
            if (renderOutDoubleBufferAVPro == null || renderOutDoubleBufferAVPro.Length < crtCount)
            {
                renderOutDoubleBufferAVPro = new bool[crtCount];
                for (int i = 0; i < crtCount; i++)
                {
                    if (renderOutCrt[i])
                        renderOutDoubleBufferAVPro[i] = renderOutCrt[i].doubleBuffered;
                }
            }

            if (renderOutDoubleBufferUnity == null || renderOutDoubleBufferUnity.Length < crtCount)
            {
                renderOutDoubleBufferUnity = new bool[crtCount];
                for (int i = 0; i < crtCount; i++)
                {
                    if (renderOutCrt[i])
                        renderOutDoubleBufferUnity[i] = renderOutCrt[i].doubleBuffered;
                }
            }

            _UpdateCRTDoubleBuffer();
        }

        void _UpdateCRTDoubleBuffer()
        {
            for (int i = 0; i < crtCount; i++)
                _UpdateCRTDoubleBuffer(i);
        }

        void _UpdateCRTDoubleBuffer(int crtIndex)
        {
            if (!renderOutCrt[crtIndex])
                return;

            bool useDB = _screenSource == VideoSource.VIDEO_SOURCE_AVPRO ? renderOutDoubleBufferAVPro[crtIndex] : renderOutDoubleBufferUnity[crtIndex];
            renderOutCrt[crtIndex].doubleBuffered = useDB;

            Material mat = renderOutCrt[crtIndex].material;
            if (mat && shaderPropDoubleBufferedList[baseIndexCrt + crtIndex] != "")
                mat.SetInt(shaderPropDoubleBufferedList[baseIndexCrt + crtIndex], useDB ? 1 : 0);
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

        void _LegacyCRTUpgrade()
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
        }

        #endregion

        #region Shared Materials

        Texture[] _originalMaterialTexture;
        int[] _originalMatAVPro;
        int[] _originalMatInvert;
        int[] _originalMatGamma;
        int[] _originalMatFit;
        float[] _originalMatAspectRatio;

        void _SetupSharedMaterials()
        {
            materialCount = materialUpdateList.Length;
            if (materialCount == 0)
                return;

            for (int i = 0; i < materialCount; i++)
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

        void _UpdateSharedMaterials()
        {
            if (materialCount == 0)
                return;

            Texture validCurrent = ValidCurrentTexture;
            int fit = _VideoScreenFit();

            for (int i = 0; i < materialCount; i++)
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
        }

        void _CaptureSharedMaterialOverrides()
        {
            if (materialCount == 0)
                return;

            _originalMaterialTexture = new Texture[materialCount];
            _originalMatAVPro = new int[materialCount];
            _originalMatInvert = new int[materialCount];
            _originalMatGamma = new int[materialCount];
            _originalMatFit = new int[materialCount];
            _originalMatAspectRatio = new float[materialCount];

            for (int i = 0; i < materialCount; i++)
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

        void _RestoreSharedMaterialOverrides()
        {
            if (materialCount == 0)
                return;

            for (int i = 0; i < materialCount; i++)
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

        #endregion

        #region Property Blocks

        void _SetupPropertyBlocks()
        {
            propBlockCount = propMeshList.Length;
            if (propBlockCount == 0)
                return;

            for (int i = 0; i < propBlockCount; i++)
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

        void _UpdatePropertyBlocks()
        {
            if (propBlockCount == 0)
                return;

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

        #endregion

        #region Global Shader Properties

        int globalPropCount = 0;
        int[] globalPropMainTexList;
        int[] globalPropAVProList;
        int[] globalPropInvertList;
        int[] globalPropGammaList;
        int[] globalPropFitList;
        int[] globalPropAspectRatioList;

        void _SetupGlobalProperties()
        {
            globalPropCount = globalPropertyList.Length;
            if (globalPropCount == 0)
                return;

            if (globalPropCount > 0)
            {
                globalPropMainTexList = new int[globalPropCount];
                globalPropAVProList = new int[globalPropCount];
                globalPropInvertList = new int[globalPropCount];
                globalPropGammaList = new int[globalPropCount];
                globalPropFitList = new int[globalPropCount];
                globalPropAspectRatioList = new int[globalPropCount];

                for (int i = 0; i < globalPropCount; i++)
                {
                    if (globalPropertyList[i] == null)
                        continue;

                    _LoadGlobalPropertyMap(i, globalPropertyList[i]);
                }
            }
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

        void _UpdateGlobalProperties()
        {
            if (globalPropCount <= 0)
                return;

            Texture validCurrent = ValidCurrentTexture;

            int fit = _VideoScreenFit();
            for (int i = 0; i < globalPropCount; i++)
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

        #endregion

        #region VRSL

        bool vrslReady = false;
        RenderTexture vrslBuffer;

        void _InitVRSL()
        {
            _CalculateVRSLReady();

            if (!vrslDmxRT)
                return;

            _RefreshVRSL();
        }

        void _UpdateVRSL()
        {
            if (!vrslBlitMat)
                return;

            Texture validCurrent = ValidCurrentTexture;

            vrslBlitMat.SetTexture("_MainTex", validCurrent);
            if (validCurrent.width > 0 && validCurrent.height > 0)
                vrslBlitMat.SetVector("_MainTexTexelSize", new Vector4(1f / validCurrent.width, 1f / validCurrent.height, validCurrent.width, validCurrent.height));

            //vrslBlitMat.SetTexture("_BufferTex", vrslBuffer);

            _SetMatIntProperty(vrslBlitMat, "_ApplyGamma", currentGamma ? 1 : 0);
            _SetMatIntProperty(vrslBlitMat, "_FlipY", currentInvert ? 1 : 0);
            _SetMatFloatProperty(vrslBlitMat, "_AspectRatio", currentAspectRatio);

            //_SetMatFloatProperty(vrslBlitMat, "_DoubleBuffered", currentAVPro && vrslDoubleBufferAVPro ? 1 : 0);
        }

        public bool VRSLEnabled
        {
            get { return vrslEnabled; }
            set
            {
                vrslEnabled = value;
                _CalculateVRSLReady();
            }
        }

        void _CalculateVRSLReady()
        {
            vrslReady = vrslEnabled && vrslDmxRT && vrslBlitMat;
        }

        void _BlitVRSL()
        {
            if (vrslReady)
            {
                if (vrslBuffer)
                    VRCGraphics.Blit(vrslDmxRT, vrslBuffer);

                //Texture tex = ValidCurrentTexture;
                VRCGraphics.Blit(null, vrslDmxRT, vrslBlitMat);
            }
        }

        void _UpdateVRSLBuffer()
        {
            if (!vrslDmxRT)
                return;

            bool shouldBuffer = false;
            if (vrslDoubleBufferAVPro && _screenSource == VideoSource.VIDEO_SOURCE_AVPRO)
                shouldBuffer = true;
            if (vrslDoubleBufferUnity && _screenSource == VideoSource.VIDEO_SOURCE_UNITY)
                shouldBuffer = true;

            if (shouldBuffer)
            {
                if (!vrslBuffer)
                {
                    vrslBuffer = new RenderTexture(vrslDmxRT.descriptor);
                    vrslBuffer.Create();
                    _DebugLog($"Initialized VRSL buffer {vrslBuffer.width}x{vrslBuffer.height}");
                }

                if (vrslBlitMat)
                {
                    vrslBlitMat.SetTexture("_BufferTex", vrslBuffer);
                    vrslBlitMat.SetInt("_DoubleBuffered", shouldBuffer ? 1 : 0);
                }
            }
            else
            {
                if (vrslBuffer)
                {
                    vrslBuffer.Release();
                    vrslBuffer = null;
                    _DebugLog("Released VRSL buffer");
                }

                if (vrslBlitMat)
                {
                    vrslBlitMat.SetTexture("_BufferTex", Texture2D.blackTexture);
                    vrslBlitMat.SetInt("_DoubleBuffered", 0);
                }
            }
        }

        void _RefreshVRSL()
        {
            if (!vrslDmxRT)
                return;

            bool horizontal = false;
            if (vrslMode == VRSLMode.Horizontal)
                horizontal = true;
            else if (vrslMode == VRSLMode.Vertical)
                horizontal = false;
            else if (vrslController)
            {
                int mode = (int)vrslController.GetProgramVariable("DMXMode");
                horizontal = mode == 0;
            }
            else
                horizontal = vrslDmxRT.height == 960;

            _UpdateVRSLBuffer();

            if (vrslBlitMat)
            {
                vrslBlitMat.SetTexture("_MainTex", vrslDmxRT);
                vrslBlitMat.SetVector("_MainTexSize", new Vector4(vrslDmxRT.width, vrslDmxRT.height, 0, 0));
                vrslBlitMat.SetVector("_MainTexTexelSize", new Vector4(1f / vrslDmxRT.width, 1f / vrslDmxRT.height, vrslDmxRT.width, vrslDmxRT.height));
                vrslBlitMat.SetVector("_OffsetScale", new Vector4(vrslOffsetScale.x, vrslOffsetScale.y, vrslOffsetScale.z, vrslOffsetScale.z));
                vrslBlitMat.SetInt("_Horizontal", horizontal ? 1 : 0);
            }
        }

        #endregion

        #region Debug

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
            debugState._SetValue("validatedTexture", validatedTexture ? validatedTexture.ToString() : "--");
            debugState._SetValue("currentTexture", currentTexture ? currentTexture.ToString() : "--");
            debugState._SetValue("currentAVPro", currentAVPro.ToString());
            debugState._SetValue("currentGamma", currentGamma.ToString());
            debugState._SetValue("currentInvert", currentInvert.ToString());
            debugState._SetValue("currentFit", currentFit.ToString());
            debugState._SetValue("currentAspectRatio", currentAspectRatio.ToString());
            debugState._SetValue("currentResolution", $"{currentRes.x}x{currentRes.y}");
            debugState._SetValue("currentValid", currentValid.ToString());
        }

        #endregion
    }
}
