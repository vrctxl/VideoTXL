
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UrlControl : UdonSharpBehaviour
    {
        public SyncPlayer syncPlayer;
        public LocalPlayer localPlayer;

        public VRCUrl url;

        public void _Trigger()
        {
            if (Utilities.IsValid(syncPlayer))
            {
                syncPlayer._ChangeUrl(url);
            }

            if (Utilities.IsValid(localPlayer))
            {
                localPlayer.streamUrl = url;
                localPlayer._TriggerStop();
                localPlayer._TriggerPlay();
            }
        }

        public override void Interact()
        {
            _Trigger();
        }
    }
}
