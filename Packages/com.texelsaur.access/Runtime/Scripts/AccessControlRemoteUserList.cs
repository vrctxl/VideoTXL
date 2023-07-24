
using Texel;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace Texel
{
    public enum ACLListFormat {
        Newline,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class AccessControlRemoteUserList : AccessControlUserSource
    {
        public AccessControl accessControl;
        public VRCUrl remoteStringUrl;
        public ACLListFormat remoteStringFormat;
        public bool allowManualRefresh = false;
        public bool allowPeriodicRefresh = false;
        public float refreshPeriod = 1800;
        public DebugLog debugLog;

        public string[] userList = new string[0];

        [UdonSynced]
        int syncRefreshCount = 0;

        protected override void _Init()
        {
            base._Init();

            _LoadFromRemote();

            if (allowPeriodicRefresh && refreshPeriod > 0)
                SendCustomEventDelayedSeconds(nameof(_PeriodicRefresh), refreshPeriod);
        }

        public override bool _ContainsName(string name)
        {
            for (int i = 0; i < userList.Length; i++)
            {
                if (userList[i] == name)
                    return true;
            }

            return false;
        }

        public int RefreshCount
        {
            get { return syncRefreshCount; }
            set
            {
                syncRefreshCount = value;
                _LoadFromRemote();
            }
        }

        public void _ManualRefresh()
        {
            if (!_AccessCheck())
                return;

            RefreshCount += 1;
            RequestSerialization();
        }

        public void _PeriodicRefresh()
        {
            if (Networking.IsOwner(gameObject))
            {
                RefreshCount += 1;
                RequestSerialization();
            }

            SendCustomEventDelayedSeconds(nameof(_PeriodicRefresh), refreshPeriod);
        }

        void _LoadFromRemote()
        {
            VRCStringDownloader.LoadUrl(remoteStringUrl, (UdonBehaviour)(Component)this);
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            _DebugLog(result.Error);
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            _DebugLog($"Received data {result.Result.Length} characters");

            userList = result.Result.Split('\n');

            _UpdateHandlers(EVENT_REVALIDATE);
        }

        public void _DebugLog(string message)
        {
            if (debugLog)
                debugLog._Write("ACLRemote", message);
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            //DebugLowLevel($"PostSerialize: {result.success}, {result.byteCount} bytes");
        }

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            if (!accessControl)
                return true;

            bool requesterCheck = accessControl._HasAccess(requestingPlayer) || Networking.IsOwner(requestingPlayer, gameObject);
            bool requesteeCheck = accessControl._HasAccess(requestedOwner);

            //DebugLowLevel($"Ownership check: requester={requesterCheck}, requestee={requesteeCheck}");

            return requesterCheck && requesteeCheck;
        }

        bool _AccessCheck()
        {
            if (accessControl && !accessControl._LocalHasAccess())
                return false;

            if (!Networking.IsOwner(gameObject))
            {
                //if (!allowOwnershipTransfer)
                //    return false;

                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            return true;
        }
    }
}
