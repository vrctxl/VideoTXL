
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class StreamControl : UdonSharpBehaviour
    {
        public AccessControl modAcl;

        public AudioOverrideZone portalAudioZone;
        public AudioOverrideZone botAudioZone;

        public Material camBoxMat;
        public Texture streamOutTex;
        public Texture streamOutOffTex;

        public Image streamOutButtonImage;
        public Text streamOutButtonText;

        [UdonSynced, FieldChangeCallback("StreamOutEnabled")]
        bool syncStreamOutEnabled = true;

        void Start()
        {
            if (Networking.IsOwner(gameObject))
            {
                StreamOutEnabled = true;
                RequestSerialization();
            }
        }

        public bool StreamOutEnabled
        {
            set
            {
                syncStreamOutEnabled = value;
                _UpdateStreamOut();
            }
            get { return syncStreamOutEnabled; }
        }

        public void _ToggleStreamOut()
        {
            if (!modAcl._LocalHasAccess())
                return;

            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            StreamOutEnabled = !StreamOutEnabled;
            RequestSerialization();
        }

        void _UpdateStreamOut()
        {
            if (StreamOutEnabled)
            {
                streamOutButtonImage.color = new Color(.3f, 1f, .3f);
                streamOutButtonText.text = "Stream Out\nENABLED";
                camBoxMat.SetTexture("_ScreenTex", streamOutTex);
            }
            else
            {
                streamOutButtonImage.color = new Color(1f, .3f, .3f);
                streamOutButtonText.text = "Stream Out\nDISABLED";
                camBoxMat.SetTexture("_ScreenTex", streamOutOffTex);
            }

            botAudioZone._SetLinkedZoneActive(portalAudioZone, StreamOutEnabled);
        }
    }
}
