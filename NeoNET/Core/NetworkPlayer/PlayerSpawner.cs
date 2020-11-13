using Mirror;
using System;
using UnityEngine;
using NeoFPS.Mirror.NetworkPlayer;

namespace NeoFPS.Mirror
{
    public class PlayerSpawner : NetworkBehaviour
    {
        [Tooltip("Character prefab to spawn.")]
        [SerializeField, RequiredObjectProperty]
        private FpsNetCharacter _characterPrefab = null;

        [Client]
        public void TryRespawn()
        {
            CmdRespawn();
        }
        [Command]
        private void CmdRespawn()
        {
            ICharacter spawned = SpawnManager.SpawnCharacter(_characterPrefab, GetComponent<IController>(), false, null);
            NetworkServer.Spawn(spawned.gameObject, base.connectionToClient);    
            RpcCharacterSpawned(spawned.GetComponent<NetworkIdentity>());
        }
        [ClientRpc]
        private void RpcCharacterSpawned(NetworkIdentity character)
        {
            ICharacter playerChar = (character == null) ? null : playerChar = character.GetComponent<ICharacter>();

            if(playerChar != null && !base.isServer){
                playerChar.controller = GetComponent<FpsNetPlayerController>();
            }
        }
    }
}