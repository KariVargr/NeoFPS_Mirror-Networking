using NeoSaveGames;
using System.Collections;
using System.Collections.Generic;
using NeoFPS.Mirror.NetworkPlayer;
using NeoFPS.Samples;
using UnityEngine;
using Mirror;

namespace NeoFPS.Mirror.Menu
{
    [HelpURL("http://docs.neofps.com/manual/samples-ui.html")]
    public class NetworkMenuRootNavControls : MenuNavControls
	{
        [SerializeField] private NetworkManager m_NetManager = null;
        [SerializeField] private MultiInputButton m_HostButton = null;
		[SerializeField] private MultiInputButton m_JoinButton = null;
        [SerializeField] private MultiInputButton m_RespawnButton = null;

        public override void Show()
        {
            base.Show();
            
            if (m_NetManager != null)
            {
				if(m_HostButton != null){
                	m_HostButton.interactable = (!m_NetManager.isNetworkActive);
                	m_HostButton.RefreshInteractable();
                    m_HostButton.gameObject.SetActive(!m_NetManager.isNetworkActive);
				}
				if(m_JoinButton != null){
                	m_JoinButton.interactable = (!m_NetManager.isNetworkActive);
                	m_JoinButton.RefreshInteractable();
                    m_JoinButton.gameObject.SetActive(!m_NetManager.isNetworkActive);
				}
                if(m_RespawnButton != null){
                    if(m_NetManager.isNetworkActive && (FpsNetCharacter.localPlayerCharacter == null || !FpsNetCharacter.localPlayerCharacter.isAlive) ){
                	    m_RespawnButton.interactable = true;
                	    m_RespawnButton.RefreshInteractable();
                        m_RespawnButton.gameObject.SetActive(true);
                    }else{
                        m_RespawnButton.interactable = false;
                	    m_RespawnButton.RefreshInteractable();
                        m_RespawnButton.gameObject.SetActive(false);
                    }
				}
            }
        }
    }
}