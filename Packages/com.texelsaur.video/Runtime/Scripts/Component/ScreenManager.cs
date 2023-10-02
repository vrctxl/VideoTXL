
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ScreenManager : EventBase
    {
        [Tooltip("A proxy for dispatching video-related events to this object")]
        public TXLVideoPlayer videoPlayer;

        [Tooltip("Log debug statements to a world object")]
        public DebugLog debugLog;
        public bool vrcLogging = false;
        public bool eventLogging = false;
        public bool lowLevelLogging = false;

        [Tooltip("Prevent screen state from cycling between loading and error placeholders.")]
        public bool latchErrorState = true;

        [Tooltip("Whether to update material assignments on a set of video screen objects")]
        public bool useMaterialOverrides = false;
        [Tooltip("Use separate playback materials for Unity and AVPro player modes.  This may be necessary for some shaders that can't be configured with an AVPro flag.")]
        public bool separatePlaybackMaterials = false;
        [Tooltip("The screen material to apply when video is playing back.  The object's current material is captured as the playback material by default.")]
        public Material playbackMaterial;
        [Tooltip("The screen material to apply when video is playing back in Unity video mode.  The object's current material is captured as the playback material by default.")]
        public Material playbackMaterialUnity;
        [Tooltip("The screen material to apply when video is playing back in AVPro mode.  The object's current material is captured as the playback material by default.")]
        public Material playbackMaterialAVPro;
        [Tooltip("The screen material to apply when no video is playing or loading.")]
        public Material logoMaterial;
        [Tooltip("The screen material to apply when a video is being loaded.  Falls back to Logo Material.")]
        public Material loadingMaterial;
        public Material syncMaterial;
        [Tooltip("The screen material to apply when an audio-only video is detected.")]
        public Material audioMaterial;
        [Tooltip("The screen material to apply when an error has occurred.  Falls back to Logo Material.")]
        public Material errorMaterial;
        [Tooltip("The screen material to apply when an invalid URL or offline stream has been loaded.  Falls back to Error Material.")]
        public Material errorInvalidMaterial;
        [Tooltip("The screen material to apply when loading has been temporarily rate-limited.  Falls back to Error Material.")]
        public Material errorRateLimitedMaterial;
        [Tooltip("The screen material to apply when an untrusted URL has been loaded and untrusted URLs are blocked.  Falls back to Error Material.")]
        public Material errorBlockedMaterial;

        [Tooltip("The screen material to apply in Unity's editor runtime")]
        public Material editorMaterial;

        public MeshRenderer[] screenMesh;
        public int[] screenMaterialIndex;

        [Tooltip("Whether to update textures properties on a set of shared material objects")]
        public bool useTextureOverrides = false;

        [Tooltip("Whether the aspect ratio for placeholder textures should be overridden by a specific value.  Use this to supply the original aspect ratio for textures that have been rescaled to powers-of-2.")]
        public bool overrideAspectRatio = true;
        [Tooltip("The aspect ratio that should be used for placeholder textures, ignoring their native unity value.")]
        public float aspectRatio = 1.777f;

        [Tooltip("The screen texture to apply when no video is playing or loading.")]
        public Texture logoTexture;
        [Tooltip("The screen texture to apply when a video is being loaded.  Falls back to Logo Texture.")]
        public Texture loadingTexture;
        public Texture syncTexture;
        [Tooltip("The screen texture to apply when an audio-only video is detected.")]
        public Texture audioTexture;
        [Tooltip("The screen texture to apply when an error has occurred.  Falls back to Logo Texture.")]
        public Texture errorTexture;
        [Tooltip("The screen texture to apply when an invalid URL or offline stream has been loaded.  Falls back to Error Texture.")]
        public Texture errorInvalidTexture;
        [Tooltip("The screen texture to apply when loading has been temporarily rate-limited.  Falls back to Error Texture.")]
        public Texture errorRateLimitedTexture;
        [Tooltip("The screen texture to apply when an untrusted URL has been loaded and untrusted URLs are blocked.  Falls back to Error Texture.")]
        public Texture errorBlockedTexture;

        [Tooltip("The screen texture to apply in Unity's editor runtime")]
        public Texture editorTexture;

        public Material[] materialUpdateList;
        public ScreenPropertyMap[] materialPropertyList;
        /*public string[] materialTexPropertyList;
        public string[] materialAVPropertyList;
        public string[] materialInvertList;
        public string[] materialGammaList;
        public string[] materialFitList;
        public string[] materialAspectRatioList;*/

        public MeshRenderer[] propMeshList;
        public int[] propMaterialOverrideList;
        public int[] propMaterialIndexList;
        public ScreenPropertyMap[] propPropertyList;
        /*public string[] propMainTexList;
        public string[] propAVProList;
        public string[] propInvertList;
        public string[] propGammaList;
        public string[] propFitList;
        public string[] propAspectRatioList;*/

        [Tooltip("Blit the source video or placeholder image to a specified custom render texture (CRT).  Each copy of the video player that writes to a CRT and could play concurrently must have its own CRT asset and associated material.")]
        public bool useRenderOut = false;
        [Tooltip("A predefined custom render texture (CRT).  The CRT should be backed by a compatible material shader, such as VideoTXL/RenderOut.")]
        public CustomRenderTexture outputCRT;
        [Tooltip("A map of properties to update on the CRT's material as the video player state changes.")]
        public ScreenPropertyMap outputMaterialProperties;

        public CustomRenderTexture[] outputCRTList;
        public ScreenPropertyMap[] outputMaterialPropertiesList;

        /*string outputMaterialMainTex = "_MainTex";
        string outputMaterialAVPro;
        string outputMaterialInvert;
        string outputMaterialGamma;
        string outputMaterialFit;
        string outputMaterialAspectRatio;*/

        int baseIndexCrt;
        int shaderPropCrtLength;
        int baseIndexMat;
        int shaderPropMatLength;
        int baseIndexProp;
        int shaderPropPropLength;

        string[] shaderPropMainTexList;
        string[] shaderPropAVProList;
        string[] shaderPropInvertList;
        string[] shaderPropGammaList;
        string[] shaderPropFitList;
        string[] shaderPropAspectRatioList;

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
        const int SCREEN_INDEX_LOGO = 1;
        const int SCREEN_INDEX_LOADING = 2;
        const int SCREEN_INDEX_SYNC = 3;
        const int SCREEN_INDEX_AUDIO = 4;
        const int SCREEN_INDEX_ERROR = 5;
        const int SCREEN_INDEX_ERROR_INVALID = 6;
        const int SCREEN_INDEX_ERROR_RATE = 7;
        const int SCREEN_INDEX_ERROR_BLOCKED = 8;
        const int SCREEN_INDEX_EDITOR = 9;
        const int SCREEN_INDEX_COUNT = 10;

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
        bool currentAVPro;
        bool currentInvert;
        bool currentGamma;
        int currentFit;
        float currentAspectRatio;
        bool currentValid = false;

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
            _DebugEvent("Init");

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
            replacementMaterials[SCREEN_INDEX_EDITOR] = editorMaterial;

            replacementTextures = new Texture[SCREEN_INDEX_COUNT];
            replacementTextures[SCREEN_INDEX_PLAYBACK] = null;
            replacementTextures[SCREEN_INDEX_LOGO] = logoTexture;
            replacementTextures[SCREEN_INDEX_LOADING] = loadingTexture ?? logoTexture;
            replacementTextures[SCREEN_INDEX_SYNC] = syncTexture ?? logoTexture;
            replacementTextures[SCREEN_INDEX_AUDIO] = audioTexture ?? logoTexture;
            replacementTextures[SCREEN_INDEX_ERROR] = errorTexture ?? logoTexture;
            replacementTextures[SCREEN_INDEX_ERROR_INVALID] = errorInvalidTexture ?? replacementTextures[SCREEN_INDEX_ERROR];
            replacementTextures[SCREEN_INDEX_ERROR_RATE] = errorRateLimitedTexture ?? replacementTextures[SCREEN_INDEX_ERROR];
            replacementTextures[SCREEN_INDEX_ERROR_BLOCKED] = errorBlockedTexture ?? replacementTextures[SCREEN_INDEX_ERROR];
            replacementTextures[SCREEN_INDEX_EDITOR] = editorTexture;

#if COMPILER_UDONSHARP
            _InitMaterialOverrides();
            _InitTextureOverrides();
#endif

            SendCustomEventDelayedFrames(nameof(_PostInit), 1);
        }

        public void _PostInit()
        {
            if (videoPlayer)
            {
                if (videoPlayer.VideoManager)
                {
                    videoPlayer.VideoManager._Register(VideoManager.SOURCE_CHANGE_EVENT, this, "_OnSourceChanged");
                    captureRenderer = videoPlayer.VideoManager.CaptureRenderer;
                    _OnSourceChanged();
                }

                videoPlayer._Register(TXLVideoPlayer.EVENT_VIDEO_STATE_UPDATE, this, "_OnVideoStateUpdate");
                _OnVideoStateUpdate();
            }
        }

        private void OnDisable()
        {

#if COMPILER_UDONSHARP
            _RestoreMaterialOverrides();
            _RestoreTextureOverrides();
#endif
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

        public int CurrentScreenFit
        {
            get { return currentFit; }
        }

        public float CurrentAspectRatioOverride
        {
            get { return currentAspectRatio; }
        }

        void _InitMaterialOverrides()
        {
            if (!useMaterialOverrides)
                return;

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
            if (!useTextureOverrides)
                return;

            bool hasMaterialUpdates = Utilities.IsValid(materialUpdateList) && materialUpdateList.Length > 0;
            bool hasPropupdates = Utilities.IsValid(propMeshList) && propMeshList.Length > 0;
            bool hasCrtUpdates = Utilities.IsValid(outputCRTList) && outputCRTList.Length > 0;

            if (!hasMaterialUpdates && !hasPropupdates && !hasCrtUpdates)
            {
                useTextureOverrides = false;
                return;
            }

            // Legacy upgrade
            if (useRenderOut && !hasCrtUpdates)
            {
                outputCRTList = new CustomRenderTexture[1];
                outputCRTList[0] = outputCRT;

                outputMaterialPropertiesList = new ScreenPropertyMap[1];
                outputMaterialPropertiesList[0] = outputMaterialProperties;

                hasCrtUpdates = true;
            }

            baseIndexCrt = 0;
            shaderPropCrtLength = outputCRTList.Length;
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
                for (int i = 0; i < outputCRTList.Length; i++)
                {
                    if (outputCRTList[i] == null)
                        continue;

                    ScreenPropertyMap propMap = outputMaterialPropertiesList[i];
                    if (propMap)
                        _LoadPropertyMap(baseIndexCrt + i, propMap);
                    else
                        _TryLoadDefaultProps(baseIndexCrt + i, outputCRTList[i].material);
                }
            }

            // Output Material
            /* if (useRenderOut) {
                if (outputMaterialProperties)
                    _LoadPropertyMap(baseIndexCrt, outputMaterialProperties);
                else if (outputCRT)
                    _TryLoadDefaultProps(baseIndexCrt, outputCRT.material);
            }*/

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
                _originalMatAVPro[i] = _GetMatIntProperty(materialUpdateList[i], shaderPropMainTexList[baseIndexMat + i]);
                _originalMatInvert[i] = _GetMatIntProperty(materialUpdateList[i], shaderPropInvertList[baseIndexMat + i]);
                _originalMatGamma[i] = _GetMatIntProperty(materialUpdateList[i], shaderPropGammaList[baseIndexMat + i]);
                _originalMatFit[i] = _GetMatIntProperty(materialUpdateList[i], shaderPropFitList[baseIndexMat + i]);
                _originalMatAspectRatio[i] = _GetMatFloatProperty(materialUpdateList[i], shaderPropAspectRatioList[baseIndexMat + i]);
            }
        }

        bool _TryLoadDefaultProps(int i, Material mat)
        {
            if (!mat || !mat.shader)
                return false;

            switch (mat.shader.name)
            {
                case "VideoTXL/RealtimeEmissiveGamma":
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
            shaderPropAspectRatioList[i] = "";
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

        public void _UpdateVideoError(VideoError error)
        {
            _lastErrorCode = error;
        }

        public void _OnVideoStateUpdate()
        {
            _DebugEvent("Event OnVideoStateUpdate");

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

        public void _OnSourceChanged()
        {
            _DebugEvent("Event OnSourceChanged");

            captureRenderer = videoPlayer.VideoManager.CaptureRenderer;
            _screenSource = videoPlayer.VideoManager.ActiveSourceType;

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

        public void _UpdateScreenMaterial(int screenMode)
        {
            _DebugLowLevel($"Update screen material: mode={screenMode}");
            if (_screenMode == screenMode && _screenFit == videoPlayer.screenFit)
                return;

            _screenMode = screenMode;
            _screenFit = videoPlayer.screenFit;

            _ResetCheckScreenMaterial();
        }

        public void _ResetCheckScreenMaterial()
        {
            _EnsureInit();

            _checkFrameCount = 0;

            _ResetCaptureData();
            currentValid = false;

            Texture captureTex = CaptureValid();
            currentValid = Utilities.IsValid(captureTex);

            int screenIndex = _CalculateScreenIndex(currentValid);

            if (useMaterialOverrides)
                _UpdateObjects(replacementMaterials[screenIndex]);

            if (useTextureOverrides)
            {
                _UpdateCaptureData(replacementTextures[screenIndex], captureTex);
                _UpdateMaterials();
                _UpdatePropertyBlocks();
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
            SendCustomEventDelayedSeconds(nameof(_CheckUpdateScreenMaterial), delay);
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
            SendCustomEventDelayedFrames(nameof(_CheckUpdateScreenMaterial), frames);
        }

        public void _CheckUpdateScreenMaterial()
        {
            pendingUpdates -= 1;

            if (_screenMode != SCREEN_MODE_NORMAL)
            {
                _QueueUpdateCheckIfNoPending(2);
                return;
            }

            _EnsureInit();

            Texture captureTex = CaptureValid();
            bool prevValid = currentValid;
            currentValid = Utilities.IsValid(captureTex);
            _DebugLowLevel($"Check Update Screen Material: valid={currentValid}");

            int screenIndex = _CalculateScreenIndex(currentValid);

            if (useMaterialOverrides)
                _UpdateObjects(replacementMaterials[screenIndex]);

            if (useTextureOverrides)
            {
                _UpdateCaptureData(replacementTextures[screenIndex], captureTex);
                _UpdateMaterials();
                _UpdatePropertyBlocks();
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

        void _ResetCaptureData()
        {
            currentTexture = null;
            currentAVPro = false;
            currentInvert = false;
            currentGamma = false;
            currentFit = videoPlayer.screenFit;
            currentAspectRatio = 0;
        }

        void _UpdateCaptureData(Texture replacementTex, Texture captureTex)
        {
            Texture lastTex = currentTexture;

            _ResetCaptureData();
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
                    currentInvert = !videoPlayer.IsQuest;
                    currentGamma = true;
                }
            }

            if (lastTex != currentTexture)
                _UpdateHandlers(EVENT_UPDATE);

            currentAspectRatio = (overrideAspectRatio && _screenMode != SCREEN_MODE_NORMAL) ? aspectRatio : 0;
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

        void _UpdateMaterials()
        {
            for (int i = 0; i < materialUpdateList.Length; i++)
            {
                Material mat = materialUpdateList[i];
                string name = shaderPropMainTexList[baseIndexMat + i];
                if (mat == null || name == null || name.Length == 0)
                    continue;

                mat.SetTexture(name, currentTexture);

                _SetMatIntProperty(mat, shaderPropAVProList[baseIndexMat + i], currentAVPro ? 1 : 0);
                _SetMatIntProperty(mat, shaderPropInvertList[baseIndexMat + i], currentGamma ? 1 : 0);
                _SetMatIntProperty(mat, shaderPropGammaList[baseIndexMat + i], currentInvert ? 1 : 0);
                _SetMatIntProperty(mat, shaderPropFitList[baseIndexMat + i], videoPlayer.screenFit);
                _SetMatFloatProperty(mat, shaderPropAspectRatioList[baseIndexMat + i], currentAspectRatio);
            }

            if (useRenderOut && Utilities.IsValid(outputCRT))
            {
                Material mat = outputCRT.material;
                if (Utilities.IsValid(mat))
                {
                    mat.SetTexture(shaderPropMainTexList[baseIndexCrt], currentTexture);

                    _SetMatIntProperty(mat, shaderPropAVProList[baseIndexCrt], currentAVPro ? 1 : 0);
                    _SetMatIntProperty(mat, shaderPropGammaList[baseIndexCrt], currentGamma ? 1 : 0);
                    _SetMatIntProperty(mat, shaderPropInvertList[baseIndexCrt], currentInvert ? 1 : 0);
                    _SetMatIntProperty(mat, shaderPropFitList[baseIndexCrt], videoPlayer.screenFit);
                    _SetMatFloatProperty(mat, shaderPropAspectRatioList[baseIndexCrt], currentAspectRatio);

                    if (_screenMode == SCREEN_MODE_NORMAL)
                        outputCRT.updateMode = CustomRenderTextureUpdateMode.Realtime;
                    else
                    {
                        outputCRT.updateMode = CustomRenderTextureUpdateMode.OnDemand;
                        outputCRT.Update();
                    }
                }
            }
        }

        void _UpdatePropertyBlocks()
        {
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

                block.SetTexture(texName, currentTexture);

                _SetIntProperty(shaderPropAVProList[baseIndexProp + i], currentAVPro ? 1 : 0);
                _SetIntProperty(shaderPropGammaList[baseIndexProp + i], currentGamma ? 1 : 0);
                _SetIntProperty(shaderPropInvertList[baseIndexProp + i], currentInvert ? 1 : 0);
                _SetIntProperty(shaderPropFitList[baseIndexProp + i], videoPlayer.screenFit);
                _SetFloatProperty(shaderPropAspectRatioList[baseIndexProp + i], currentAspectRatio);

                if (useMatIndex)
                    renderer.SetPropertyBlock(block, propMaterialIndexList[i]);
                else
                    renderer.SetPropertyBlock(block);
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
            if (!separatePlaybackMaterials)
                return Utilities.IsValid(playbackMaterial) ? playbackMaterial : _originalScreenMaterial[meshIndex];

            if (_screenSource == VideoSource.VIDEO_SOURCE_UNITY && Utilities.IsValid(playbackMaterialUnity))
                return playbackMaterialUnity;
            if (_screenSource == VideoSource.VIDEO_SOURCE_AVPRO && Utilities.IsValid(playbackMaterialAVPro))
                return playbackMaterialAVPro;

            return _originalScreenMaterial[meshIndex];
        }

        public Texture CaptureValid()
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

        void _DebugEvent(string message)
        {
            if (eventLogging)
                _DebugLog(message);
        }

        void _DebugLowLevel(string message)
        {
            if (lowLevelLogging)
                _DebugLog(message);
        }
    }
}
