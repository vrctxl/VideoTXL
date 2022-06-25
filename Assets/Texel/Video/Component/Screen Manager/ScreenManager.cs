
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [AddComponentMenu("Texel/VideoTXL/Screen Manager")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ScreenManager : UdonSharpBehaviour
    {
        [Tooltip("A proxy for dispatching video-related events to this object")]
        public VideoPlayerProxy dataProxy;

        [Tooltip("Log debug statements to a world object")]
        public DebugLog debugLog;

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
        public MeshRenderer videoCaptureRenderer;
        public MeshRenderer streamCaptureRenderer;
        [Tooltip("The material capturing the video or stream source")]
        public Material captureMaterial;
        [Tooltip("The name of the property holding the main texture in the capture material")]
        public string captureTextureProperty;
        [Tooltip("The render texture receiving data from a unity video component")]
        public RenderTexture captureRT;

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
        public string[] materialTexPropertyList;
        public string[] materialAVPropertyList;
        public string[] materialInvertList;
        public string[] materialGammaList;
        public string[] materialFitList;

        public MeshRenderer[] propMeshList;
        public int[] propMaterialOverrideList;
        public int[] propMaterialIndexList;
        public ScreenPropertyMap[] propPropertyList;
        public string[] propMainTexList;
        public string[] propAVProList;
        public string[] propInvertList;
        public string[] propGammaList;
        public string[] propFitList;

        [Tooltip("Blit the source video or placeholder image to a specified custom render texture (CRT).  Each copy of the video player that writes to a CRT and could play concurrently must have its own CRT asset and associated material.")]
        public bool useRenderOut = false;
        [Tooltip("A predefined custom render texture (CRT).  The CRT should be backed by a compatible material shader, such as VideoTXL/RenderOut.")]
        public CustomRenderTexture outputCRT;
        [Tooltip("A map of properties to update on the CRT's material as the video player state changes.")]
        public ScreenPropertyMap outputMaterialProperties;

        Material[] _originalScreenMaterial;
        Texture[] _originalMaterialTexture;

        public const int SCREEN_SOURCE_NONE = 0;
        public const int SCREEN_SOURCE_AVPRO = 1;
        public const int SCREEN_SOURCE_UNITY = 2;

        public const int SCREEN_MODE_UNINITIALIZED = -1;
        public const int SCREEN_MODE_NORMAL = 0;
        public const int SCREEN_MODE_LOGO = 1;
        public const int SCREEN_MODE_LOADING = 2;
        public const int SCREEN_MODE_ERROR = 3;
        public const int SCREEN_MODE_AUDIO = 4;
        public const int SCREEN_MODE_SYNC = 5;

        const int PLAYER_STATE_STOPPED = 0;
        const int PLAYER_STATE_LOADING = 1;
        const int PLAYER_STATE_PLAYING = 2;
        const int PLAYER_STATE_ERROR = 3;

        bool _initComplete = false;
        int _screenSource = SCREEN_SOURCE_UNITY;
        int _screenMode = SCREEN_MODE_UNINITIALIZED;
        int _screenFit = 0;
        VideoError _lastErrorCode = 0;
        int _checkFrameCount = 0;
        MaterialPropertyBlock block;

        Texture currentTexture;
        bool currentAVPro;
        bool currentInvert;
        bool currentGamma;
        int currentFit;
        bool currentValid = false;

        const int eventCount = 3;
        const int UPDATE_EVENT = 0;
        const int CAPTURE_VALID_EVENT = 1;

        int[] handlerCount;
        Component[][] handlers;
        string[][] handlerEvents;

        bool texOverrideValidAVPro = false;
        bool texOverrideValidUnity = false;

        void Start()
        {
            if (Utilities.IsValid(dataProxy))
                dataProxy._RegisterEventHandler(this, "_VideoStateUpdate");

            _Init();
        }

        public void _Init()
        {
            if (_initComplete)
                return;

            block = new MaterialPropertyBlock();

            handlerCount = new int[eventCount];
            handlers = new Component[eventCount][];
            handlerEvents = new string[eventCount][];

            for (int i = 0; i < eventCount; i++)
            {
                handlers[i] = new Component[0];
                handlerEvents[i] = new string[0];
            }

#if COMPILER_UDONSHARP
            _InitMaterialOverrides();
            _InitTextureOverrides();
#endif

            _initComplete = true;
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
            bool hasPropupdates = Utilities.IsValid(propMainTexList) && propMainTexList.Length > 0;

            if (!hasMaterialUpdates && !hasPropupdates)
            {
                useTextureOverrides = false;
                return;
            }

            texOverrideValidAVPro = Utilities.IsValid(streamCaptureRenderer) && Utilities.IsValid(captureTextureProperty) && captureTextureProperty != "";

            texOverrideValidUnity = Utilities.IsValid(videoCaptureRenderer) && Utilities.IsValid(captureTextureProperty) && captureTextureProperty != "";
            texOverrideValidUnity |= Utilities.IsValid(captureRT);

            if (!texOverrideValidAVPro && !texOverrideValidUnity)
            {
                useTextureOverrides = false;
                return;
            }

            materialInvertList = new string[materialUpdateList.Length];
            materialGammaList = new string[materialUpdateList.Length];
            materialFitList = new string[materialUpdateList.Length];

            // Material Props
            for (int i = 0; i < materialUpdateList.Length; i++)
            {
                if (materialUpdateList[i] == null)
                    continue;

                ScreenPropertyMap map = materialPropertyList[i];
                if (Utilities.IsValid(map))
                {
                    materialTexPropertyList[i] = _ConditionalCopy(materialTexPropertyList[i], map.screenTexture);
                    materialAVPropertyList[i] = _ConditionalCopy(materialAVPropertyList[i], map.avProCheck);
                    materialInvertList[i] = map.invertY;
                    materialGammaList[i] = map.applyGamma;
                    materialFitList[i] = map.screenFit;
                }
            }

            propInvertList = new string[propMeshList.Length];
            propGammaList = new string[propMeshList.Length];
            propFitList = new string[propMeshList.Length];

            // Property Block Props
            for (int i = 0; i < propMeshList.Length; i++)
            {
                if (propMeshList[i] == null)
                    continue;

                ScreenPropertyMap map = propPropertyList[i];
                if (Utilities.IsValid(map))
                {
                    propMainTexList[i] = _ConditionalCopy(propMainTexList[i], map.screenTexture);
                    propAVProList[i] = _ConditionalCopy(propAVProList[i], map.avProCheck);
                    propInvertList[i] = map.invertY;
                    propGammaList[i] = map.applyGamma;
                    propFitList[i] = map.screenFit;
                }
            }

            // Capture original material textures
            _originalMaterialTexture = new Texture[materialUpdateList.Length];
            for (int i = 0; i < materialUpdateList.Length; i++)
            {
                if (materialUpdateList[i] == null)
                    continue;

                string name = materialTexPropertyList[i];
                if (name == null || name.Length == 0)
                    continue;

                _originalMaterialTexture[i] = materialUpdateList[i].GetTexture(name);
            }
        }

        string _ConditionalCopy(string source, string replace)
        {
            if (Utilities.IsValid(source) && source.Length > 0)
                return source;
            return replace;
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
                string name = materialTexPropertyList[i];
                if (mat == null || name == null || name.Length == 0)
                    continue;

                mat.SetTexture(name, _originalMaterialTexture[i]);
                string avProProp = materialAVPropertyList[i];
                if (avProProp != null && avProProp.Length > 0)
                    mat.SetInt(avProProp, 0);
            }
        }

        public void _VideoStateUpdate()
        {
            _UpdateScreenSource(dataProxy.playerSource);

            switch (dataProxy.playerState)
            {
                case PLAYER_STATE_STOPPED:
                    _UpdateScreenMaterial(SCREEN_MODE_LOGO);
                    break;
                case PLAYER_STATE_LOADING:
                    _UpdateScreenMaterial(SCREEN_MODE_LOADING);
                    break;
                case PLAYER_STATE_PLAYING:
                    _UpdateScreenMaterial(dataProxy.syncing ? SCREEN_MODE_SYNC : SCREEN_MODE_NORMAL);
                    break;
                case PLAYER_STATE_ERROR:
                    _lastErrorCode = dataProxy.lastErrorCode;
                    _UpdateScreenMaterial(SCREEN_MODE_ERROR);
                    break;
            }
        }

        public void _UpdateVideoError(VideoError error)
        {
            _lastErrorCode = error;
        }

        public void _UpdateScreenSource(int source)
        {
            _screenSource = source;
        }

        Material _GetReplacementMaterial(bool captureValid)
        {
            Material replacementMat = null;
            switch (_screenMode)
            {
                case SCREEN_MODE_LOGO:
                    replacementMat = logoMaterial;
                    break;
                case SCREEN_MODE_LOADING:
                    replacementMat = loadingMaterial;
                    break;
                case SCREEN_MODE_SYNC:
                    replacementMat = syncMaterial;
                    break;
                case SCREEN_MODE_ERROR:
                    if (_lastErrorCode == VideoError.AccessDenied)
                        replacementMat = errorBlockedMaterial;
                    else if (_lastErrorCode == VideoError.InvalidURL)
                        replacementMat = errorInvalidMaterial;
                    else if (_lastErrorCode == VideoError.RateLimited)
                        replacementMat = errorRateLimitedMaterial;

                    if (replacementMat == null)
                        replacementMat = errorMaterial;
                    break;
                case SCREEN_MODE_NORMAL:
                default:
                    replacementMat = null;
                    break;
            }

            // Try to detect audio-only source
            if (replacementMat == null && !captureValid)
            {
                // Will fill in with audio material on future check cycle
                replacementMat = loadingMaterial;
            }

            return replacementMat;
        }

        Texture _GetReplacemenTexture(bool captureValid)
        {
            Texture replacementTex = null;
            switch (_screenMode)
            {
                case SCREEN_MODE_LOGO:
                    replacementTex = logoTexture;
                    break;
                case SCREEN_MODE_LOADING:
                    replacementTex = loadingTexture;
                    break;
                case SCREEN_MODE_SYNC:
                    replacementTex = syncTexture;
                    break;
                case SCREEN_MODE_ERROR:
                    if (_lastErrorCode == VideoError.AccessDenied)
                        replacementTex = errorBlockedTexture;
                    else if (_lastErrorCode == VideoError.InvalidURL)
                        replacementTex = errorInvalidTexture;
                    else if (_lastErrorCode == VideoError.RateLimited)
                        replacementTex = errorRateLimitedTexture;

                    if (replacementTex == null)
                        replacementTex = errorTexture;
                    break;
                case SCREEN_MODE_NORMAL:
                default:
                    replacementTex = null;
                    break;
            }

            // Try to detect audio-only source
            if (replacementTex == null && !captureValid)
            {
                // Will fill in with audio material on future check cycle
                replacementTex = loadingTexture;
            }

            return replacementTex;
        }

        public void _UpdateScreenMaterial(int screenMode)
        {
            if (!_initComplete)
                _Init();

            if (_screenMode == screenMode && _screenFit == dataProxy.screenFit)
                return;

            _screenMode = screenMode;
            _screenFit = dataProxy.screenFit;
            _checkFrameCount = 0;

            _ResetCaptureData();

            Texture captureTex = CaptureValid();
            currentValid = Utilities.IsValid(captureTex);
            bool usingVideoSource = _screenMode == SCREEN_MODE_NORMAL;

            if (useMaterialOverrides)
            {
                Material replacementMat = _GetReplacementMaterial(currentValid);
                usingVideoSource |= replacementMat == null;

                _UpdateObjects(replacementMat);
            }

            if (useTextureOverrides)
            {
                Texture replacementTex = _GetReplacemenTexture(currentValid);
                usingVideoSource |= replacementTex == null;

                _UpdateCaptureData(replacementTex, captureTex);
                _UpdateMaterials();
                _UpdatePropertyBlocks();
            }

            //#if !UNITY_EDITOR
            if (usingVideoSource)
            {
                if (!currentValid)
                    SendCustomEventDelayedFrames("_CheckUpdateScreenMaterial", 1);
                else
                {
                    DebugLog("Capture valid");
                    _UpdateHandlers(CAPTURE_VALID_EVENT);
                    SendCustomEventDelayedFrames("_CheckUpdateScreenMaterial", 2);
                }
            }
            //#endif
        }

        public void _CheckUpdateScreenMaterial()
        {
            if (!_initComplete)
                _Init();
            if (_screenMode != SCREEN_MODE_NORMAL)
                return;

            Texture captureTex = CaptureValid();
            bool prevValid = currentValid;
            currentValid = Utilities.IsValid(captureTex);

            if (useMaterialOverrides)
            {
                Material replacementMat = null;
                if (!currentValid)
                {
                    if (loadingMaterial != null && _checkFrameCount < 10)
                        replacementMat = loadingMaterial;
                    else if (audioMaterial != null && _checkFrameCount >= 10)
                        replacementMat = audioMaterial;
                    else if (logoMaterial != null)
                        replacementMat = logoMaterial;
                }

                _UpdateObjects(replacementMat);
            }

            if (useTextureOverrides)
            {
                Texture replacementTex = null;
                if (!currentValid)
                {
                    if (loadingTexture != null && _checkFrameCount < 10)
                        replacementTex = loadingTexture;
                    else if (audioTexture != null && _checkFrameCount >= 10)
                        replacementTex = audioTexture;
                    else if (logoTexture != null)
                        replacementTex = logoTexture;
                }

                _UpdateCaptureData(replacementTex, captureTex);
                _UpdateMaterials();
                _UpdatePropertyBlocks();
            }

            if (!currentValid)
            {
                _checkFrameCount += 1;
                int delay = _checkFrameCount < 100 ? 1 : 10;
                SendCustomEventDelayedFrames("_CheckUpdateScreenMaterial", delay);
            }
            else
            {
                if (!prevValid)
                {
                    DebugLog("Capture valid");
                    _UpdateHandlers(CAPTURE_VALID_EVENT);
                }
                SendCustomEventDelayedSeconds("_CheckUpdateScreenMaterial", 2);
            }
        }

        void _ResetCaptureData()
        {
            currentTexture = null;
            currentAVPro = false;
            currentInvert = false;
            currentGamma = false;
            currentFit = dataProxy.screenFit;
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

                if (_screenSource == SCREEN_SOURCE_AVPRO && texOverrideValidAVPro)
                {
                    currentTexture = captureTex;
                    currentAVPro = true;
                }
                else if (_screenSource == SCREEN_SOURCE_UNITY && texOverrideValidUnity)
                    currentTexture = captureTex;

                if (currentAVPro)
                {
                    currentInvert = !dataProxy.quest;
                    currentGamma = true;
                }
            }

            if (lastTex != currentTexture)
                _UpdateHandlers(UPDATE_EVENT);
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
                string name = materialTexPropertyList[i];
                if (mat == null || name == null || name.Length == 0)
                    continue;

                mat.SetTexture(name, currentTexture);

                _SetMatIntProperty(mat, materialAVPropertyList[i], currentAVPro ? 1 : 0);
                _SetMatIntProperty(mat, materialGammaList[i], currentGamma ? 1 : 0);
                _SetMatIntProperty(mat, materialInvertList[i], currentInvert ? 1 : 0);
                _SetMatIntProperty(mat, materialFitList[i], dataProxy.screenFit);
            }

            if (useRenderOut && Utilities.IsValid(outputCRT))
            {
                Material mat = outputCRT.material;
                if (Utilities.IsValid(mat))
                {
                    if (Utilities.IsValid(outputMaterialProperties))
                    {
                        mat.SetTexture(outputMaterialProperties.screenTexture, currentTexture);

                        _SetMatIntProperty(mat, outputMaterialProperties.avProCheck, currentAVPro ? 1 : 0);
                        _SetMatIntProperty(mat, outputMaterialProperties.applyGamma, currentGamma ? 1 : 0);
                        _SetMatIntProperty(mat, outputMaterialProperties.invertY, currentInvert ? 1 : 0);
                        _SetMatIntProperty(mat, outputMaterialProperties.screenFit, dataProxy.screenFit);
                    } else
                        mat.SetTexture("_MainTex", currentTexture);

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
                string texName = propMainTexList[i];
                if (renderer == null || name == null || name.Length == 0)
                    continue;

                bool useMatIndex = propMaterialOverrideList[i] == 1;
                if (useMatIndex)
                    renderer.GetPropertyBlock(block, propMaterialIndexList[i]);
                else
                    renderer.GetPropertyBlock(block);

                block.SetTexture(texName, currentTexture);

                _SetIntProperty(propAVProList[i], currentAVPro ? 1 : 0);
                _SetIntProperty(propGammaList[i], currentGamma ? 1 : 0);
                _SetIntProperty(propInvertList[i], currentInvert ? 1 : 0);
                _SetIntProperty(propFitList[i], dataProxy.screenFit);

                renderer.SetPropertyBlock(block);
            }
        }

        void _SetIntProperty(string prop, int value)
        {
            if (prop != null && prop.Length > 0)
                block.SetInt(prop, value);
        }

        void _SetMatIntProperty(Material mat, string prop, int value)
        {
            if (prop != null && prop.Length > 0)
                mat.SetInt(prop, value);
        }

        Material _GetPlaybackMaterial(int meshIndex)
        {
            if (!separatePlaybackMaterials)
                return Utilities.IsValid(playbackMaterial) ? playbackMaterial : _originalScreenMaterial[meshIndex];

            if (_screenSource == SCREEN_SOURCE_UNITY && Utilities.IsValid(playbackMaterialUnity))
                return playbackMaterialUnity;
            if (_screenSource == SCREEN_SOURCE_AVPRO && Utilities.IsValid(playbackMaterialAVPro))
                return playbackMaterialAVPro;

            return _originalScreenMaterial[meshIndex];
        }

        public Texture CaptureValid()
        {
            if (_screenSource == SCREEN_SOURCE_AVPRO && Utilities.IsValid(streamCaptureRenderer))
            {
                Material mat = streamCaptureRenderer.sharedMaterial;
                if (mat == null)
                    return null;

                Texture tex = mat.GetTexture("_MainTex");
                if (tex == null)
                    return null;
                if (tex.width < 16 || tex.height < 16)
                    return null;

                if (!currentValid)
                    DebugLog($"Resolution {tex.width} x {tex.height}");
                return tex;
            }

            if (_screenSource == SCREEN_SOURCE_UNITY && Utilities.IsValid(videoCaptureRenderer))
            {
                videoCaptureRenderer.GetPropertyBlock(block);
                Texture tex = block.GetTexture("_MainTex");
                if (tex == null)
                    return null;
                if (tex.width < 16 || tex.height < 16)
                    return null;

                if (!currentValid)
                    DebugLog($"Resolution {tex.width} x {tex.height}");
                return tex;
            }

            if (_screenSource == SCREEN_SOURCE_UNITY && Utilities.IsValid(captureRT))
                return captureRT;

            return null;
        }

        public void _RegisterUpdate(Component handler, string eventName)
        {
            _Register(UPDATE_EVENT, handler, eventName);
            useTextureOverrides = true;
        }

        public void _RegisterCaptureValid(Component handler, string eventName)
        {
            _Register(CAPTURE_VALID_EVENT, handler, eventName);
        }

        void _Register(int eventIndex, Component handler, string eventName)
        {
            if (!Utilities.IsValid(handler) || !Utilities.IsValid(eventName))
                return;

            _Init();

            for (int i = 0; i < handlerCount[eventIndex]; i++)
            {
                if (handlers[eventIndex][i] == handler)
                    return;
            }

            handlers[eventIndex] = (Component[])_AddElement(handlers[eventIndex], handler, typeof(Component));
            handlerEvents[eventIndex] = (string[])_AddElement(handlerEvents[eventIndex], eventName, typeof(string));

            handlerCount[eventIndex] += 1;
        }

        void _UpdateHandlers(int eventIndex)
        {
            for (int i = 0; i < handlerCount[eventIndex]; i++)
            {
                UdonBehaviour script = (UdonBehaviour)handlers[eventIndex][i];
                script.SendCustomEvent(handlerEvents[eventIndex][i]);
            }
        }

        Array _AddElement(Array arr, object elem, Type type)
        {
            Array newArr;
            int count = 0;

            if (Utilities.IsValid(arr))
            {
                count = arr.Length;
                newArr = Array.CreateInstance(type, count + 1);
                Array.Copy(arr, newArr, count);
            }
            else
                newArr = Array.CreateInstance(type, 1);

            newArr.SetValue(elem, count);
            return newArr;
        }

        void DebugLog(string message)
        {
            Debug.Log("[VideoTXL:ScreenManager] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("ScreenManager", message);
        }
    }
}
