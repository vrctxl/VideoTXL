
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Texel
{
    [AddComponentMenu("Texel/General/Zone Membership")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ZoneMembership : UdonSharpBehaviour
    {
        [NonSerialized]
        public VRCPlayerApi playerEventArg;

        int[] players = new int[100];
        int maxIndex = -1;

        void Start()
        {

        }

        public void _PlayerTriggerEnter()
        {
            _AddPlayer(playerEventArg);
        }

        public void _PlayerTriggerExit()
        {
            _RemovePlayer(playerEventArg);
        }

        public void _AddPlayer(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player))
                return;

            if (!player.IsValid())
            {
                _RemovePlayer(player);
                return;
            }

            int id = player.playerId;
            for (int i = 0; i <= maxIndex; i++)
            {
                if (players[i] == id)
                    return;
            }

            maxIndex += 1;
            players[maxIndex] = id;
        }

        public void _RemovePlayer(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player))
                return;

            int id = player.playerId;
            for (int i = 0; i <= maxIndex; i++)
            {
                if (players[i] == id)
                {
                    players[i] = players[maxIndex];
                    maxIndex -= 1;
                    return;
                }
            }
        }

        public int _PlayerCount()
        {
            return maxIndex + 1;
        }

        public VRCPlayerApi _GetPlayer(int index)
        {
            if (index < 0 || index > maxIndex)
                return null;

            int id = players[index];
            if (id == -1)
                return null;

            return VRCPlayerApi.GetPlayerById(id);
        }

        public bool _ContainsPlayer(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player))
                return false;

            int id = player.playerId;
            for (int i = 0; i <= maxIndex; i++)
            {
                if (players[i] == id)
                    return true;
            }

            return false;
        }
    }
}
