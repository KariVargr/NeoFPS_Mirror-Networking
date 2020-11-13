using System;
using System.Collections;
using System.Collections.Generic;
using NeoSaveGames;
using NeoSaveGames.Serialization;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

namespace NeoFPS.Mirror.NetworkPlayer
{
    public class FpsNetGameMinimal : FpsGameMode
    {
        [SerializeField, Tooltip("Should the game mode automatically spawn a player character immediately on start.")]
        private bool m_ServerAuthoritative = false;

        public bool ServerAuthoritative
        {
            get{ return m_ServerAuthoritative;}
            private set
            {
                m_ServerAuthoritative = value;
            }
        }

        [SerializeField, Tooltip("Should the game mode automatically spawn a player character immediately on start.")]
        private bool m_SpawnOnStart = true;

        [SerializeField, Tooltip("How long after dying does the game react (gives time for visual feedback).")]
        private float m_DeathSequenceDuration = 5f;

        [SerializeField, Tooltip("What to do when the player character dies.")]
        private DeathAction m_DeathAction = DeathAction.Respawn;

        private WaitForSeconds m_DeathSequenceYield = null;

        public enum DeathAction
        {
            Respawn,
            EventCall,
            MainMenu,
            ContinueFromCheckpoint
        }

        [SerializeField] private UnityEvent m_OnDeathEvent = new UnityEvent();

        private IController m_Player = null;
        public IController player
        {
            get { return m_Player; }
            protected set
            {
                // Unsubscribe from old player events
                if (m_Player != null)
                    m_Player.onCharacterChanged -= OnPlayerCharacterChanged;

                // Set new player
                m_Player = value;

                // Track player for persistence
                /*var playerComponent = m_Player as Component;
                if (playerComponent != null)
                    m_PersistentObjects[0] = playerComponent.GetComponent<NeoSerializedGameObject>();
                else
                    m_PersistentObjects[0] = null;*/

                // Subscribe to player events
                if (m_Player != null)
                {
                    m_Player.onCharacterChanged += OnPlayerCharacterChanged;
                    OnPlayerCharacterChanged(m_Player.currentCharacter);
                }
            }
        }

        public void RemotePlayerSet(FpsNetPlayerController data)
        {
            // Setup up to only allow 1 remote set
            if(player == null)
                player = data;

            if(m_SpawnOnStart)
                data.networkInstance.PlayerSpawner.TryRespawn();
                
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            m_DeathSequenceDuration = Mathf.Clamp(m_DeathSequenceDuration, 0f, 300f);
        }
#endif

        protected override void Awake()
        {
            base.Awake();
            m_DeathSequenceYield = new WaitForSeconds(m_DeathSequenceDuration);
        }

        protected override void OnStart()
        {
            base.OnStart();
            inGame = true;
        }

        protected override void OnDestroy()
        {
            if (m_PlayerCharacter != null)
            {
                m_PlayerCharacter.onIsAliveChanged -= OnPlayerCharacterIsAliveChanged;
                m_PlayerCharacter = null;
            }
            base.OnDestroy();
        }

        protected override IController InstantiatePlayer ()
		{
            Debug.Log(this.name + ": InstantiatePlayer() called but is not a Vaild way to spawn playerController");
            return null;
		}

		protected override ICharacter GetPlayerCharacterPrototype (IController player)
		{
			Debug.Log(this.name + ": GetPlayerCharacterPrototype() No Needed for Net Game Modes");
            return null;
		}

        protected virtual void OnPlayerCharacterIsAliveChanged(ICharacter character, bool alive)
        {
            if (inGame && !alive)
            {
                IController player = character.controller;
                StartCoroutine(DelayedDeathReactionCoroutine(player));
            }
        }

        private IEnumerator DelayedDeathReactionCoroutine(IController player)
        {
            // Wait for death sequence to complete
            if (m_DeathSequenceDuration > 0f)
                yield return m_DeathSequenceYield;
            else
                yield return null;

            // Respawn
            if (inGame)
            {
                switch (m_DeathAction)
                {
                    case DeathAction.Respawn:
                        if(player is FpsNetPlayerController netPlayer)
                            netPlayer.networkInstance.PlayerSpawner.TryRespawn();
                        break;
                    case DeathAction.EventCall:
                        if(m_OnDeathEvent != null)
                            m_OnDeathEvent.Invoke();
                        break;
                    case DeathAction.MainMenu:
                        SceneManager.LoadScene(0);
                        //Might need some network shutdown here
                        break;
                    case DeathAction.ContinueFromCheckpoint:
                        Debug.Log("Not Implamented");
                        break;
                }
            }
        }

        protected override void ProcessOldPlayerCharacter(ICharacter oldCharacter)
        {
            if (oldCharacter != null)
                Destroy(oldCharacter.gameObject);
        }

        public override void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            base.ReadProperties(reader, nsgo);
        }

        public override void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            base.WriteProperties(writer, nsgo, saveMode);
        }

        #region PLAYER

        private ICharacter m_PlayerCharacter = null;
        public ICharacter playerCharacter
        {
            get { return m_PlayerCharacter; }
        }

        protected virtual void OnPlayerCharacterChanged(ICharacter character)
        {
            // Unsubscribe from old character events
            if (m_PlayerCharacter != null)
            {
                m_PlayerCharacter.onIsAliveChanged -= OnPlayerCharacterIsAliveChanged;
                ProcessOldPlayerCharacter(m_PlayerCharacter);
            }

            // Set new character
            m_PlayerCharacter = character;

            // Track character for persistence
            /*var characterComponent = m_PlayerCharacter as Component;
            if (characterComponent != null)
                m_PersistentObjects[1] = characterComponent.GetComponent<NeoSerializedGameObject>();
            else
                m_PersistentObjects[1] = null;*/

            // Subscribe to character events
            if (m_PlayerCharacter != null)
            {
                m_PlayerCharacter.onIsAliveChanged += OnPlayerCharacterIsAliveChanged;
                OnPlayerCharacterIsAliveChanged(m_PlayerCharacter, m_PlayerCharacter.isAlive);
            }
        }

        #endregion

        #region PERSISTENCE

        private NeoSerializedGameObject[] m_PersistentObjects = new NeoSerializedGameObject[2];

        protected override NeoSerializedGameObject[] GetPersistentObjects()
        {
            if (m_PersistentObjects[0] != null && m_PersistentObjects[1] != null)
                return m_PersistentObjects;
            else
            {
                Debug.Log("No Persistence Save Objects");
                Debug.Log("m_PersistentObjects[0] != null: " + (m_PersistentObjects[0] != null));
                Debug.Log("m_PersistentObjects[1] != null: " + (m_PersistentObjects[1] != null));
                return null;
            }
        }

        protected override void SetPersistentObjects(NeoSerializedGameObject[] objects)
        {
            var controller = objects[0].GetComponent<IController>();
            if (controller != null)
            {
                player = controller;

                var character = objects[1].GetComponent<ICharacter>();
                if (character != null)
                    player.currentCharacter = character;
            }
        }

        #endregion
    }
}