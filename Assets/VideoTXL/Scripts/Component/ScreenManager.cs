
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDKBase;
using VRC.Udon;
using Texel;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEditorInternal;
using UdonSharpEditor;
#endif

namespace VideoTXL
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
        [Tooltip("The material capturing the video or stream source")]
        public Material captureMaterial;
        [Tooltip("The name of the property holding the main texture in the capture material")]
        public string captureTextureProperty;

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
        public string[] materialTexPropertyList;
        public string[] materialAVPropertyList;

        Material[] _originalScreenMaterial;
        Texture[] _originalMaterialTexture;

        public const int SCREEN_SOURCE_UNITY = 0;
        public const int SCREEN_SOURCE_AVPRO = 1;

        public const int SCREEN_MODE_NORMAL = 0;
        public const int SCREEN_MODE_LOGO = 1;
        public const int SCREEN_MODE_LOADING = 2;
        public const int SCREEN_MODE_ERROR = 3;
        public const int SCREEN_MODE_AUDIO = 4;
        public const int SCREEN_MODE_SYNC = 5;

        const int PLAYER_STATE_STOPPED = 0x01;
        const int PLAYER_STATE_LOADING = 0x02;
        const int PLAYER_STATE_SYNC = 0x04;
        const int PLAYER_STATE_PLAYING = 0x08;
        const int PLAYER_STATE_ERROR = 0x10;
        const int PLAYER_STATE_PAUSED = 0x20;

        bool _initComplete = false;
        int _screenSource = SCREEN_SOURCE_AVPRO;
        int _screenMode = SCREEN_MODE_NORMAL;
        VideoError _lastErrorCode = 0;
        int _checkFrameCount = 0;

        void Start()
        {
            if (Utilities.IsValid(dataProxy))
                dataProxy._RegisterEventHandler(gameObject, "_VideoStateUpdate");

            _Init();
        }

        public void _Init()
        {
            if (_initComplete)
                return;

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

            if (!Utilities.IsValid(materialUpdateList) || materialUpdateList.Length == 0)
            {
                useTextureOverrides = false;
                return;
            }

            if (!Utilities.IsValid(captureMaterial) || !Utilities.IsValid(captureTextureProperty) || captureTextureProperty == "")
            {
                useTextureOverrides = false;
                return;
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
            switch (dataProxy.playerState)
            {
                case PLAYER_STATE_STOPPED:
                    _UpdateScreenMaterial(SCREEN_MODE_LOGO);
                    break;
                case PLAYER_STATE_LOADING:
                    _UpdateScreenMaterial(SCREEN_MODE_LOADING);
                    break;
                case PLAYER_STATE_SYNC:
                    _UpdateScreenMaterial(SCREEN_MODE_SYNC);
                    break;
                case PLAYER_STATE_PLAYING:
                    _UpdateScreenMaterial(SCREEN_MODE_NORMAL);
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

            _screenMode = screenMode;
            _checkFrameCount = 0;

            bool captureValid = CaptureValid();

            if (useMaterialOverrides)
            {
                Material replacementMat = _GetReplacementMaterial(captureValid);
                
#if UNITY_EDITOR
                if (editorMaterial != null)
                    replacementMat = editorMaterial;
#endif
                // Update all screen meshes with correct display material
                for (int i = 0; i < screenMesh.Length; i++)
                {
                    int index = screenMaterialIndex[i];
                    if (index < 0)
                        continue;

                    Material newMat = replacementMat;
                    if (newMat == null)
                        newMat = _originalScreenMaterial[i];
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

            if (useTextureOverrides)
            {
                Texture replacementTex = _GetReplacemenTexture(captureValid);
                
#if UNITY_EDITOR
                if (editorTexture != null)
                    replacementTex = editorTexture;
#endif
                Texture tex = replacementTex;
                int avPro = 0;

                if (replacementTex == null)
                {
                    tex = captureMaterial.GetTexture(captureTextureProperty);
                    if (_screenSource == SCREEN_SOURCE_AVPRO)
                        avPro = 1;
                }

                // Update all extra screen materials with correct predefined or captured texture
                for (int i = 0; i < materialUpdateList.Length; i++)
                {
                    Material mat = materialUpdateList[i];
                    string name = materialTexPropertyList[i];
                    if (mat == null || name == null || name.Length == 0)
                        continue;

                    mat.SetTexture(name, tex);
                    string avProProp = materialAVPropertyList[i];
                    if (avProProp != null && avProProp.Length > 0)
                        mat.SetInt(avProProp, avPro);
                }
            }

#if !UNITY_EDITOR
            if (!captureValid)
                SendCustomEventDelayedFrames("_CheckUpdateScreenMaterial", 1);
            else
                DebugLog("Capture valid");
#endif
        }

        public void _CheckUpdateScreenMaterial()
        {
            if (!_initComplete)
                _Init();
            if (_screenMode != SCREEN_MODE_NORMAL)
                return;

            bool captureValid = CaptureValid();

            if (useMaterialOverrides)
            {
                Material replacementMat = null;
                if (!captureValid)
                {
                    if (loadingMaterial != null && _checkFrameCount < 10)
                        replacementMat = loadingMaterial;
                    else if (audioMaterial != null && _checkFrameCount >= 10)
                        replacementMat = audioMaterial;
                    else if (logoMaterial != null)
                        replacementMat = logoMaterial;
                }

#if UNITY_EDITOR
                if (editorMaterial != null)
                    replacementMat = editorMaterial;
#endif

                for (int i = 0; i < screenMesh.Length; i++)
                {
                    int index = screenMaterialIndex[i];
                    if (index < 0)
                        continue;

                    Material newMat = replacementMat;
                    if (newMat == null)
                        newMat = _originalScreenMaterial[i];

                    if (newMat != null)
                    {
                        Material[] materials = screenMesh[i].sharedMaterials;
                        materials[index] = newMat;
                        screenMesh[i].sharedMaterials = materials;
                    }
                }
            }

            if (useTextureOverrides)
            {
                Texture replacementTex = null;
                if (!captureValid)
                {
                    if (loadingTexture != null && _checkFrameCount < 10)
                        replacementTex = loadingTexture;
                    else if (audioTexture != null && _checkFrameCount >= 10)
                        replacementTex = audioTexture;
                    else if (logoTexture != null)
                        replacementTex = logoTexture;
                }

#if UNITY_EDITOR
                if (editorTexture != null)
                    replacementTex = editorTexture;
#endif

                Texture tex = replacementTex;
                int avPro = 0;

                if (replacementTex == null)
                {
                    tex = captureMaterial.GetTexture(captureTextureProperty);
                    if (_screenSource == SCREEN_SOURCE_AVPRO)
                        avPro = 1;
                }

                for (int i = 0; i < materialUpdateList.Length; i++)
                {
                    Material mat = materialUpdateList[i];
                    string name = materialTexPropertyList[i];
                    if (mat == null || name == null || name.Length == 0)
                        continue;

                    mat.SetTexture(name, tex);
                    string avProProp = materialAVPropertyList[i];
                    if (avProProp != null && avProProp.Length > 0)
                        mat.SetInt(avProProp, avPro);
                }
            }

            if (!captureValid)
            {
                _checkFrameCount += 1;
                int delay = _checkFrameCount < 100 ? 1 : 10;
                SendCustomEventDelayedFrames("_CheckUpdateScreenMaterial", delay);
            }
            else
                DebugLog("Capture valid");
        }

        bool CaptureValid()
        {
            if (Utilities.IsValid(captureMaterial) && captureTextureProperty.Length > 0)
            {
                Texture tex = captureMaterial.GetTexture(captureTextureProperty);
                if (tex == null)
                    return false;
                else if (tex.width < 16 || tex.height < 16)
                    return false;
            }
            return true;
        }

        void DebugLog(string message)
        {
            Debug.Log("[VideoTXL:ScreenManager] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("ScreenManager", message);
        }
    }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(ScreenManager))]
    internal class ScreenManagerInspector : Editor
    {
        static bool _showErrorMatFoldout;
        static bool _showErrorTexFoldout;
        static bool _showScreenListFoldout;
        static bool[] _showScreenFoldout = new bool[0];
        static bool _showMaterialListFoldout;
        static bool[] _showMaterialFoldout = new bool[0];

        SerializedProperty dataProxyProperty;

        SerializedProperty debugLogProperty;

        SerializedProperty useMaterialOverrideProperty;
        SerializedProperty logoMaterialProperty;
        SerializedProperty loadingMaterialProperty;
        SerializedProperty syncMaterialProperty;
        SerializedProperty audioMaterialProperty;
        SerializedProperty errorMaterialProperty;
        SerializedProperty errorInvalidMaterialProperty;
        SerializedProperty errorBlockedMaterialProperty;
        SerializedProperty errorRateLimitedMaterialProperty;
        SerializedProperty editorMaterialProperty;

        SerializedProperty screenMeshListProperty;
        SerializedProperty screenMatIndexListProperty;

        SerializedProperty captureMaterialProperty;
        SerializedProperty captureTexturePropertyProperty;

        SerializedProperty useTextureOverrideProperty;
        SerializedProperty logoTextureProperty;
        SerializedProperty loadingTextureProperty;
        SerializedProperty syncTextureProperty;
        SerializedProperty audioTextureProperty;
        SerializedProperty errorTextureProperty;
        SerializedProperty errorInvalidTextureProperty;
        SerializedProperty errorBlockedTextureProperty;
        SerializedProperty errorRateLimitedTextureProperty;
        SerializedProperty editorTextureProperty;

        SerializedProperty materialUpdateListProperty;
        SerializedProperty materialTexPropertyListProperty;
        SerializedProperty materialAVPropertyListProperty;

        private void OnEnable()
        {
            dataProxyProperty = serializedObject.FindProperty(nameof(ScreenManager.dataProxy));

            debugLogProperty = serializedObject.FindProperty(nameof(ScreenManager.debugLog));

            useMaterialOverrideProperty = serializedObject.FindProperty(nameof(ScreenManager.useMaterialOverrides));
            logoMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.logoMaterial));
            loadingMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.loadingMaterial));
            syncMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.syncMaterial));
            audioMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.audioMaterial));
            errorMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.errorMaterial));
            errorInvalidMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.errorInvalidMaterial));
            errorBlockedMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.errorBlockedMaterial));
            errorRateLimitedMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.errorRateLimitedMaterial));
            editorMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.editorMaterial));

            screenMeshListProperty = serializedObject.FindProperty(nameof(ScreenManager.screenMesh));
            screenMatIndexListProperty = serializedObject.FindProperty(nameof(ScreenManager.screenMaterialIndex));

            captureMaterialProperty = serializedObject.FindProperty(nameof(ScreenManager.captureMaterial));
            captureTexturePropertyProperty = serializedObject.FindProperty(nameof(ScreenManager.captureTextureProperty));

            useTextureOverrideProperty = serializedObject.FindProperty(nameof(ScreenManager.useTextureOverrides));
            logoTextureProperty = serializedObject.FindProperty(nameof(ScreenManager.logoTexture));
            loadingTextureProperty = serializedObject.FindProperty(nameof(ScreenManager.loadingTexture));
            syncTextureProperty = serializedObject.FindProperty(nameof(ScreenManager.syncTexture));
            audioTextureProperty = serializedObject.FindProperty(nameof(ScreenManager.audioTexture));
            errorTextureProperty = serializedObject.FindProperty(nameof(ScreenManager.errorTexture));
            errorInvalidTextureProperty = serializedObject.FindProperty(nameof(ScreenManager.errorInvalidTexture));
            errorBlockedTextureProperty = serializedObject.FindProperty(nameof(ScreenManager.errorBlockedTexture));
            errorRateLimitedTextureProperty = serializedObject.FindProperty(nameof(ScreenManager.errorRateLimitedTexture));
            editorTextureProperty = serializedObject.FindProperty(nameof(ScreenManager.editorTexture));

            materialUpdateListProperty = serializedObject.FindProperty(nameof(ScreenManager.materialUpdateList));
            materialTexPropertyListProperty = serializedObject.FindProperty(nameof(ScreenManager.materialTexPropertyList));
            materialAVPropertyListProperty = serializedObject.FindProperty(nameof(ScreenManager.materialAVPropertyList));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            EditorGUILayout.PropertyField(dataProxyProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Optional Components", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(debugLogProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Object Material Overrides", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useMaterialOverrideProperty);
            if (useMaterialOverrideProperty.boolValue)
            {
                EditorGUILayout.PropertyField(logoMaterialProperty);
                EditorGUILayout.PropertyField(loadingMaterialProperty);
                EditorGUILayout.PropertyField(syncMaterialProperty);
                EditorGUILayout.PropertyField(audioMaterialProperty);
                EditorGUILayout.PropertyField(errorMaterialProperty);

                _showErrorMatFoldout = EditorGUILayout.Foldout(_showErrorMatFoldout, "Error Material Overrides");
                if (_showErrorMatFoldout)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(errorInvalidMaterialProperty);
                    EditorGUILayout.PropertyField(errorBlockedMaterialProperty);
                    EditorGUILayout.PropertyField(errorRateLimitedMaterialProperty);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(editorMaterialProperty);

                EditorGUILayout.Space();
                ScreenFoldout();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Material Texture Overrides", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useTextureOverrideProperty);
            if (useTextureOverrideProperty.boolValue)
            {
                EditorGUILayout.PropertyField(captureMaterialProperty);
                if (captureMaterialProperty.objectReferenceValue != null)
                    EditorGUILayout.PropertyField(captureTexturePropertyProperty);

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(logoTextureProperty);
                EditorGUILayout.PropertyField(loadingTextureProperty);
                EditorGUILayout.PropertyField(syncTextureProperty);
                EditorGUILayout.PropertyField(audioTextureProperty);
                EditorGUILayout.PropertyField(errorTextureProperty);

                _showErrorTexFoldout = EditorGUILayout.Foldout(_showErrorTexFoldout, "Error Texture Overrides");
                if (_showErrorTexFoldout)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(errorInvalidTextureProperty);
                    EditorGUILayout.PropertyField(errorBlockedTextureProperty);
                    EditorGUILayout.PropertyField(errorRateLimitedTextureProperty);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(editorTextureProperty);

                EditorGUILayout.Space();
                MaterialFoldout();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void ScreenFoldout()
        {
            _showScreenListFoldout = EditorGUILayout.Foldout(_showScreenListFoldout, "Video Screen Objects");
            if (_showScreenListFoldout)
            {
                EditorGUI.indentLevel++;
                int newCount = Mathf.Max(0, EditorGUILayout.DelayedIntField("Size", screenMeshListProperty.arraySize));
                if (newCount != screenMeshListProperty.arraySize)
                    screenMeshListProperty.arraySize = newCount;
                if (newCount != screenMatIndexListProperty.arraySize)
                    screenMatIndexListProperty.arraySize = newCount;

                if (_showScreenFoldout.Length != screenMeshListProperty.arraySize)
                    _showScreenFoldout = new bool[screenMeshListProperty.arraySize];

                for (int i = 0; i < screenMeshListProperty.arraySize; i++)
                {
                    _showScreenFoldout[i] = EditorGUILayout.Foldout(_showScreenFoldout[i], "Screen " + i);
                    if (_showScreenFoldout[i])
                    {
                        EditorGUI.indentLevel++;

                        SerializedProperty mesh = screenMeshListProperty.GetArrayElementAtIndex(i);
                        SerializedProperty matIndex = screenMatIndexListProperty.GetArrayElementAtIndex(i);

                        EditorGUILayout.PropertyField(mesh, new GUIContent("Mesh Renderer"));
                        EditorGUILayout.PropertyField(matIndex, new GUIContent("Material Index"));

                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        private void MaterialFoldout()
        {
            _showMaterialListFoldout = EditorGUILayout.Foldout(_showMaterialListFoldout, "Video Screen Materials");
            if (_showMaterialListFoldout)
            {
                EditorGUI.indentLevel++;
                int newCount = Mathf.Max(0, EditorGUILayout.DelayedIntField("Size", materialUpdateListProperty.arraySize));
                if (newCount != materialUpdateListProperty.arraySize)
                    materialUpdateListProperty.arraySize = newCount;
                if (newCount != materialTexPropertyListProperty.arraySize)
                    materialTexPropertyListProperty.arraySize = newCount;
                if (newCount != materialAVPropertyListProperty.arraySize)
                    materialAVPropertyListProperty.arraySize = newCount;

                if (_showMaterialFoldout.Length != materialUpdateListProperty.arraySize)
                    _showMaterialFoldout = new bool[materialUpdateListProperty.arraySize];

                for (int i = 0; i < materialUpdateListProperty.arraySize; i++)
                {
                    _showMaterialFoldout[i] = EditorGUILayout.Foldout(_showMaterialFoldout[i], "Material " + i);
                    if (_showMaterialFoldout[i])
                    {
                        EditorGUI.indentLevel++;

                        SerializedProperty matUpdate = materialUpdateListProperty.GetArrayElementAtIndex(i);
                        SerializedProperty matTexProperty = materialTexPropertyListProperty.GetArrayElementAtIndex(i);
                        SerializedProperty matAVProperty = materialAVPropertyListProperty.GetArrayElementAtIndex(i);

                        EditorGUILayout.PropertyField(matUpdate, new GUIContent("Material"));
                        EditorGUILayout.PropertyField(matTexProperty, new GUIContent("Texture Property"));
                        EditorGUILayout.PropertyField(matAVProperty, new GUIContent("AVPro Check Property"));

                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }
        }
    }
#endif
}
