using System.Collections;
using System.Collections.Generic;
using NeoSaveGames;
using NeoSaveGames.Serialization;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeoFPS.Mirror.NetworkPlayer
{
    [RequireComponent(typeof(NeoFPSNetworkManager))]
    public class FpsNetGameMinimal : FpsGameMode
    {
        private NeoFPSNetworkManager m_NetworkManager;
        [SerializeField, Tooltip("Should the game mode automatically spawn a player character immediately on start.")]
        private bool m_ServerAuthority  = false;
        [SerializeField, Tooltip("Should the game mode automatically spawn a player character immediately on start.")]
        private bool m_SpawnOnStart = true;
        [SerializeField, Tooltip("How long after dying does the game react (gives time for visual feedback).")]
        private float m_DeathSequenceDuration = 5f;

        [SerializeField, Tooltip(".")]
        private DeathAction m_DeathAction = DeathAction.Respawn;

        private WaitForSeconds m_DeathSequenceYield = null;

        public enum DeathAction
        {
            Respawn,
            ReloadScene,
            MainMenu,
            ContinueFromSave
        }

        public bool ServerAuthority
        {
            get{ return m_ServerAuthority;}
            private set
            {
                m_ServerAuthority = value;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            m_DeathSequenceDuration = Mathf.Clamp(m_DeathSequenceDuration, 0f, 300f);
            m_NetworkManager = GetComponent<NeoFPSNetworkManager>();
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

        protected override IController InstantiatePlayer ()
		{
            Debug.Log(this.name + ": InstantiatePlayer() called but is not a Vaild way to spawn playerController");
            return null;
		}

		protected override ICharacter GetPlayerCharacterPrototype (IController player)
		{
			Debug.Log(this.name + ": GetPlayerCharacterPrototype() called but is not a Vaild way to get PlayerChar Prototype");
            return null;
		}
        protected override void ProcessOldPlayerCharacter(ICharacter oldCharacter)
        {
            Debug.Log(this.name + ": ProcessOldPlayerCharacter() called but is not a Vaild way");
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
                        Respawn(player);
                        break;
                    case DeathAction.MainMenu:
                        SceneManager.LoadScene(0);
                        break;
                    case DeathAction.ContinueFromSave:
                        if (SaveGameManager.canContinue)
                            SaveGameManager.Continue();
                        else
                            SceneManager.LoadScene(0);
                        break;
                }
            }
        }
    }
}