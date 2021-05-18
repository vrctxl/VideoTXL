
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace VideoTXL
{
    public class AudioDebugPanel : UdonSharpBehaviour
    {
        public AudioSource audioSource;
        public GameObject audioGrip;
        MeshRenderer audioGripRenderer;
        public GameObject moveablePanel;

        public Text audioSourceNameText;
        public Text audioSourceDistanceText;

        public Toggle enabledToggle;
        public Toggle mutedToggle;
        public Toggle spatializedToggle;
        public Toggle postEffectToggle;
        public Toggle visibleToggle;

        public Slider volumeSlider;
        public Text volumeValueText;

        public Slider blendSlider;
        public Text blendValueText;
        public Toggle blendCurveToggle;

        public Slider reverbSlider;
        public Text reverbValueText;
        public Toggle reverbCurveToggle;

        public Slider spreadSlider;
        public Text spreadValueText;
        public Toggle spreadCurveToggle;

        public Text minDistLabel;
        public Slider minDistSlider;
        public Text minDistValueText;
        public Toggle volumeCurveToggle;

        public Slider maxDistSlider;
        public Text maxDistValueText;

        public Toggle logRolloffToggle;
        public Toggle linearRolloffToggle;
        public Toggle customRolloffToggle;

        Color gray = new Color(.4f, .4f, .4f);
        bool _inUpdate = false;
        float _lastDistance = 0;

        void Start()
        {
            audioGripRenderer = audioGrip.GetComponent<MeshRenderer>();

            _InitializePanel();
            SendCustomEventDelayedSeconds("_InitializePanel", 5);
        }

        public void _InitializePanel()
        {
            _inUpdate = true;

            string path = audioSource.name;
            if (path.Length > 60)
                path = "..." + path.Substring(path.Length - 60, 60);
            audioSourceNameText.text = "Audio Source: " + path;

            enabledToggle.isOn = audioSource.enabled;
            mutedToggle.isOn = audioSource.mute;
            spatializedToggle.isOn = audioSource.spatialize;
            postEffectToggle.isOn = audioSource.spatializePostEffects;
            visibleToggle.isOn = audioGripRenderer.enabled;

            audioGrip.transform.position = audioSource.transform.position;

            volumeSlider.value = audioSource.volume;
            volumeValueText.text = audioSource.volume.ToString();

            blendSlider.value = audioSource.spatialBlend;
            blendValueText.text = audioSource.spatialBlend.ToString();
            AnimationCurve blendCurve = audioSource.GetCustomCurve(AudioSourceCurveType.SpatialBlend);
            blendCurveToggle.isOn = blendCurve != null && blendCurve.length > 1;

            reverbSlider.value = audioSource.reverbZoneMix;
            reverbValueText.text = audioSource.reverbZoneMix.ToString();
            AnimationCurve reverbCurve = audioSource.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix);
            reverbCurveToggle.isOn = reverbCurve != null && reverbCurve.length > 1;

            spreadSlider.value = audioSource.spread;
            spreadValueText.text = audioSource.spread.ToString();
            AnimationCurve spreadCurve = audioSource.GetCustomCurve(AudioSourceCurveType.Spread);
            spreadCurveToggle.isOn = spreadCurve != null && spreadCurve.length > 1;

            minDistSlider.value = audioSource.minDistance;
            minDistValueText.text = audioSource.minDistance.ToString();
            AnimationCurve volumeCurve = audioSource.GetCustomCurve(AudioSourceCurveType.CustomRolloff);
            volumeCurveToggle.isOn = audioSource.rolloffMode == AudioRolloffMode.Custom;

            maxDistSlider.value = audioSource.maxDistance;
            maxDistValueText.text = audioSource.maxDistance.ToString();

            logRolloffToggle.isOn = audioSource.rolloffMode == AudioRolloffMode.Logarithmic;
            linearRolloffToggle.isOn = audioSource.rolloffMode == AudioRolloffMode.Linear;
            customRolloffToggle.isOn = audioSource.rolloffMode == AudioRolloffMode.Custom;

            UpdateLabels();

            _inUpdate = false;
        }

        private void Update()
        {
            float dist = Vector3.Distance(moveablePanel.transform.position, audioSource.transform.position);
            if (dist != _lastDistance)
            {
                _lastDistance = dist;
                audioSourceDistanceText.text = "Distance: " + dist.ToString("F3") + "m";
            }
        }

        public void _OnAudioSettingsChanged()
        {
            _InitializePanel();
        }

        public void _RefreshPressed()
        {
            _InitializePanel();
        }

        public void _EnableToggled()
        {
            if (_inUpdate)
                return;

            audioSource.enabled = enabledToggle.isOn;
        }

        public void _MuteToggled()
        {
            if (_inUpdate)
                return;

            audioSource.mute = mutedToggle.isOn;
        }

        public void _SpatialToggled()
        {
            if (_inUpdate)
                return;

            audioSource.spatialize = spatializedToggle.isOn;
        }

        public void _EffectsToggled()
        {
            if (_inUpdate)
                return;

            audioSource.spatializePostEffects = postEffectToggle.isOn;
        }

        public void _VisibleToggled()
        {
            if (_inUpdate)
                return;

            audioGripRenderer.enabled = visibleToggle.isOn;
        }

        public void _VolumeSliderChanged()
        {
            if (_inUpdate)
                return;

            float volume = volumeSlider.value;
            audioSource.volume = volume;
            volumeValueText.text = volume.ToString();
        }

        public void _BlendSliderChanged()
        {
            if (_inUpdate)
                return;

            float blend = blendSlider.value;
            audioSource.spatialBlend = blend;
            blendValueText.text = blend.ToString();

            AnimationCurve blendCurve = audioSource.GetCustomCurve(AudioSourceCurveType.SpatialBlend);
            blendCurveToggle.isOn = blendCurve != null && blendCurve.length > 1;
        }

        public void _ReverbSliderChanged()
        {
            if (_inUpdate)
                return;

            float reverb = reverbSlider.value;
            audioSource.reverbZoneMix = reverb;
            reverbValueText.text = reverb.ToString();

            AnimationCurve reverbCurve = audioSource.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix);
            reverbCurveToggle.isOn = reverbCurve != null && reverbCurve.length > 1;
        }

        public void _SpreadSliderChanged()
        {
            if (_inUpdate)
                return;

            float spread = spreadSlider.value;
            audioSource.spread = spread;
            spreadValueText.text = spread.ToString();

            AnimationCurve spreadCurve = audioSource.GetCustomCurve(AudioSourceCurveType.Spread);
            spreadCurveToggle.isOn = spreadCurve != null && spreadCurve.length > 1;
        }

        public void _MinDistSliderChanged()
        {
            if (_inUpdate)
                return;

            float minDist = minDistSlider.value;
            audioSource.minDistance = minDist;
            minDistValueText.text = minDist.ToString();
        }

        public void _MaxDistSliderChanged()
        {
            if (_inUpdate)
                return;

            float maxDist = maxDistSlider.value;
            audioSource.maxDistance = maxDist;
            maxDistValueText.text = maxDist.ToString();
        }

        public void _RolloffToggled()
        {
            if (_inUpdate)
                return;

            AudioRolloffMode mode = audioSource.rolloffMode;
            if (logRolloffToggle.isOn)
                mode = AudioRolloffMode.Logarithmic;
            else if (linearRolloffToggle.isOn)
                mode = AudioRolloffMode.Linear;
            else if (customRolloffToggle.isOn)
                mode = AudioRolloffMode.Custom;
            audioSource.rolloffMode = mode;

            volumeCurveToggle.isOn = audioSource.rolloffMode == AudioRolloffMode.Custom;

            UpdateLabels();
        }

        void UpdateLabels()
        {
            if (audioSource.rolloffMode == AudioRolloffMode.Custom)
                minDistLabel.color = minDistValueText.color = gray;
            else
                minDistLabel.color = minDistValueText.color = Color.black;
        }
    }
}