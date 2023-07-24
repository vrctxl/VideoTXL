
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PortalManager : UdonSharpBehaviour
    {
        public VRCStation station;

        public AccessControl botAcl;
        public AccessControl modAcl;
        public AudioOverrideZone botAudioZone;

        public GameObject botCamBox;

        public GameObject[] botObjects;
        public GameObject[] botDisableObjects;
        public GameObject[] modObjects;
        public GameObject[] botActiveObjects;
        public GameObject[] botInactiveObjects;

        [UdonSynced, FieldChangeCallback("BotActive")]
        bool syncBotActive = false;

        void Start()
        {
            SendCustomEventDelayedFrames("_Init", 1);
        }

        public void _Init()
        {
            if (botAcl)
            {
                botAcl._Register(AccessControl.EVENT_VALIDATE, this, nameof(_ValidateBotACL));
                _ValidateBotACL();
            }

            if (modAcl)
            {
                modAcl._Register(AccessControl.EVENT_VALIDATE, this, nameof(_ValidateModACL));
                _ValidateModACL();
            }

            botCamBox.SetActive(false);

            _UpdateBotActiveState();
        }

        public void _ValidateBotACL()
        {
            bool botAccess = botAcl._LocalHasAccess();

            foreach (GameObject obj in botObjects)
            {
                if (obj)
                    obj.SetActive(botAccess);
            }
            foreach (GameObject obj in botDisableObjects)
            {
                if (obj)
                    obj.SetActive(!botAccess);
            }
        }

        public void _ValidateModACL()
        {
            bool modAccess = modAcl._LocalHasAccess();

            foreach (GameObject obj in modObjects)
            {
                if (obj)
                    obj.SetActive(modAccess);
            }

            _ValidateBotACL();
        }

        public bool BotActive
        {
            get { return syncBotActive; }
            set
            {
                syncBotActive = value;
                _UpdateBotActiveState();
            }
        }

        public void _StationEnter()
        {
            if (!botAcl._LocalHasAccess())
            {
                station.ExitStation(Networking.LocalPlayer);
                return;
            }

            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            BotActive = true;
            RequestSerialization();

            botCamBox.SetActive(true);

            if (Utilities.IsValid(botAudioZone))
            {
                botAudioZone.playerArg = Networking.LocalPlayer;
                botAudioZone._PlayerEnter();
            }
        }

        public void _StationExit()
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            BotActive = false;
            RequestSerialization();

            botCamBox.SetActive(false);
            Networking.LocalPlayer.Immobilize(false);

            if (Utilities.IsValid(botAudioZone))
            {
                botAudioZone.playerArg = Networking.LocalPlayer;
                botAudioZone._PlayerLeave();
            }
        }

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            return botAcl._HasAccess(requestingPlayer) || requestingPlayer.isMaster;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (player == Networking.LocalPlayer)
            {
                BotActive = false;
                RequestSerialization();
            }
        }

        public void _TryEnterStation()
        {
            if (!botAcl._LocalHasAccess())
                return;

            Networking.LocalPlayer.TeleportTo(station.transform.position, station.transform.rotation);
            SendCustomEventDelayedFrames(nameof(_TryEnterStationDelay), 3);
        }

        public void _TryEnterStationDelay()
        {
            station.UseStation(Networking.LocalPlayer);
        }

        void _UpdateBotActiveState()
        {
            bool active = BotActive;
            foreach (GameObject obj in botActiveObjects)
            {
                if (obj)
                    obj.SetActive(active);
            }
            foreach (GameObject obj in botInactiveObjects)
            {
                if (obj)
                    obj.SetActive(!active);
            }

            if (botAudioZone)
            {
                Collider collider = botAudioZone.GetComponent<Collider>();
                if (collider)
                    collider.enabled = active;
            }
        }
    }
}
