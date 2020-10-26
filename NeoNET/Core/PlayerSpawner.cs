using Mirror;
using System;
using UnityEngine;
using NeoFPS.Mirror.NetworkPlayer;

namespace NeoFPS.Mirror
{
    public class PlayerSpawner : NetworkBehaviour
    {
        #region Types.
        public class PlayerData
        {
            public NetworkIdentity NetworkIdentity;
            //public Health Health;
        }
        #endregion

        #region Public.
        /// <summary>
        /// Dispatched when the character is updated.
        /// </summary>
        public static event Action<GameObject> OnCharacterUpdated;
        /// <summary>
        /// Data about the currently spawned player.
        /// </summary>
        public PlayerData SpawnedCharacterData { get; private set; } = new PlayerData();
        #endregion

        #region Serialized.
        /// <summary>
        /// Character prefab to spawn.
        /// </summary>
        [Tooltip("Character prefab to spawn.")]
        [SerializeField]
        private FpsNetCharacter _characterPrefab;

        #endregion

        /// <summary>
        /// Tries to respawn the player.
        /// </summary>
        [Client]
        public void TryRespawn()
        {
            CmdRespawn();
        }

        /// <summary>
        /// Sets up SpawnedCharacterData using a gameObject.
        /// </summary>
        /// <param name="go"></param>
        private void SetupSpawnedCharacterData(GameObject go)
        {
            SpawnedCharacterData.NetworkIdentity = go.GetComponent<NetworkIdentity>();
            //SpawnedCharacterData.Health = go.GetComponent<Health>();
        }


        /// <summary>
        /// Request a respawn from the server.
        /// </summary>
        [Command]
        private void CmdRespawn()
        {
            
            //If the character is not spawned yet.
            if (SpawnedCharacterData.NetworkIdentity == null)
            {
                ICharacter spawned = SpawnManager.SpawnCharacter(_characterPrefab, null, false, null);
                
                NetworkServer.Spawn(spawned.gameObject, base.connectionToClient);
                
                SetupSpawnedCharacterData(spawned.gameObject);
                TargetCharacterSpawned(SpawnedCharacterData.NetworkIdentity);
            }
            //Character is already spawned.
            else
            {
                var spawnPoint = SpawnManager.GetNextSpawnPoint(false);
                SpawnedCharacterData.NetworkIdentity.transform.position = spawnPoint.transform.position;
                SpawnedCharacterData.NetworkIdentity.transform.rotation = spawnPoint.transform.rotation;
                Physics.SyncTransforms();
                //Restore health and set respawned.
                //SpawnedCharacterData.Health.RestoreHealth();
                //SpawnedCharacterData.Health.Respawned();
            }
        }

        /// <summary>
        /// Received when the server has spawned the character.
        /// </summary>
        /// <param name="character"></param>
        [TargetRpc]
        private void TargetCharacterSpawned(NetworkIdentity character)
        {
            GameObject playerObj = (character == null) ? null : playerObj = character.gameObject;
            OnCharacterUpdated?.Invoke(playerObj);

            //If player was spawned.
            if (playerObj != null)
                SetupSpawnedCharacterData(character.gameObject);
        }

    }


}