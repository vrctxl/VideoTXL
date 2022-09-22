
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UrlRemapper : UdonSharpBehaviour
    {
        public VRCUrl[] referenceUrls;
        public VRCUrl[] remappedUrls;
        public bool[] applyPC;
        public bool[] applyQuest;

        int gameMode = GAME_MODE_PC;

        public const int GAME_MODE_PC = 0;
        public const int GAME_MODE_QUEST = 1;

        public void _SetGameMode(int mode)
        {
            gameMode = mode;
        }

        public VRCUrl _Remap(VRCUrl input)
        {
            if (!Utilities.IsValid(input))
                return input;

            string inputStr = input.Get();
            for (int i = 0; i < referenceUrls.Length; i++)
            {
                VRCUrl reffed = referenceUrls[i];
                if (!Utilities.IsValid(reffed) || inputStr != reffed.Get())
                    continue;

                if (gameMode == GAME_MODE_PC && (applyPC.Length <= i || !applyPC[i]))
                    continue;
                if (gameMode == GAME_MODE_QUEST && (applyQuest.Length <= i || !applyQuest[i]))
                    continue;

                if (remappedUrls.Length <= i || !Utilities.IsValid(remappedUrls[i]))
                    continue;

                VRCUrl mapped = remappedUrls[i];
                if (mapped.Get() == "")
                    continue;

                return remappedUrls[i];
            }

            return input;
        }
    }
}
