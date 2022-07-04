
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
            bool botAccess = botAcl._LocalHasAccess();
            foreach (GameObject obj in botObjects)
                obj.SetActive(botAccess);
            foreach (GameObject obj in botDisableObjects)
                obj.SetActive(!botAccess);

            bool modAccess = modAcl._LocalHasAccess();
            foreach (GameObject obj in modObjects)
                obj.SetActive(modAccess);

            botCamBox.SetActive(false);

            _UpdateBotActiveState();

            if (!botAccess || !Utilities.IsValid(station))
                return;
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
        }

        public void _StationExit()
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            BotActive = false;
            RequestSerialization();

            botCamBox.SetActive(false);
            Networking.LocalPlayer.Immobilize(false);
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
            if (botAcl._LocalHasAccess())
                station.UseStation(Networking.LocalPlayer);
        }

        void _UpdateBotActiveState()
        {
            bool active = BotActive;
            foreach (GameObject obj in botActiveObjects)
                obj.SetActive(active);
            foreach (GameObject obj in botInactiveObjects)
                obj.SetActive(!active);
        }
    }
}
