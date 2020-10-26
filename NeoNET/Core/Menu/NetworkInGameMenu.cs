using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using NeoFPS.Samples;
using NeoFPS.Mirror.NetworkPlayer;
using NeoFPS;
using Mirror;

namespace NeoFPS.Mirror.Menu
{
    [HelpURL("http://docs.neofps.com/manual/samples-ui.html")]
	public class NetworkInGameMenu : BaseMenu
	{
		[SerializeField] private MenuNavControls m_StartingNavControls = null;
        [SerializeField] private CanvasGroup m_HudGroup = null;
        [SerializeField] private InGameMenuBackground m_Background = null;
        [SerializeField] private int m_MainMenuScene = 0;
		[SerializeField][Range (0f, 1f)] private float m_HudAlpha = 0.25f;
        [SerializeField] private NetworkManager m_NetManager;

#if UNITY_EDITOR
        void OnValidate ()
		{
			if (m_Background == null)
				m_Background = GetComponentInChildren<InGameMenuBackground> ();
		}
		#endif

		protected override void Start ()
		{
			base.Start ();
			NeoFpsInputManager.PushEscapeHandler (Show);
			if (m_Background != null)
				m_Background.gameObject.SetActive (false);
		}

		protected override void OnDestroy ()
		{
			base.OnDestroy ();
			NeoFpsInputManager.PopEscapeHandler (Show);
        }

		public override void Show ()
        {
            NeoFpsInputManager.PushEscapeHandler(Hide);
			ShowNavControls (m_StartingNavControls);
			HidePanel ();
			base.Show ();
			CaptureInput ();
			// Fade Hud
			if (m_HudGroup != null)
				m_HudGroup.alpha = m_HudAlpha;
        }

		public override void Hide ()
        {
			base.Hide ();
			ReleaseInput ();
			// Show Hud
			if (m_HudGroup != null)
				m_HudGroup.alpha = 1f;
            NeoFpsInputManager.PopEscapeHandler(Hide);
        }

		public void OnPressExit ()
		{
			ConfirmationPopup.ShowPopup ("Are you sure you want to quit?", OnExitYes, OnExitNo);
		}

		void OnExitYes ()
		{
            if (NetworkServer.active && NetworkClient.isConnected)
                m_NetManager.StopHost();
            else if (NetworkClient.isConnected)
                m_NetManager.StopClient();
            else if (NetworkServer.active)
                m_NetManager.StopServer();
            
            SceneManager.LoadScene (m_MainMenuScene);
		}

		void OnExitNo ()
		{
		}

		void CaptureInput ()
		{
			if (m_Background != null)
				m_Background.gameObject.SetActive (true);
		}

		void ReleaseInput ()
		{
			m_Background.gameObject.SetActive (false);
		}
        public void OnJoinServer()
        {
            //PlayAudio(MenuAudio.ClickValid);
            
            if (m_NetManager != null)
            {
                TextFieldPopup.ShowPopup(
                    "Enter host IP:",
                    m_NetManager.networkAddress,
                    "Join Server",
                    (string networkAddress) =>
                    {
                        m_NetManager.networkAddress = networkAddress;
                        OnJoinSubmit();
                    }
                );
            }
        }

		void OnJoinSubmit ()
		{
			m_NetManager.StartClient();
			SpinnerPopup.ShowPopup(
				"Connecting to Host",
				() => 
				{
					while(!NetworkClient.isConnected)
					{
						if(!NetworkClient.active)
							return true;
					}
					return true;
				},
				() => 
				{ 
					if(!NetworkClient.active)
						InfoPopup.ShowPopup("Connection Failed", () => {} );
					else
						this.Hide();
				}
			);
		}

		public void OnSpawnCall()
		{
			FpsNetPlayerController.localPlayer.networkInstance.PlayerSpawner.TryRespawn();
			Hide();
		}

        public void OnHostServer()
        {
            //PlayAudio(MenuAudio.ClickValid);
            
            if (m_NetManager != null)
            {
                m_NetManager.StartHost();
				Hide();
				Show();
            }
        }
	}
}