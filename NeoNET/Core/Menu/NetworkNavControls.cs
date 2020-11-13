using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.Samples;
using Mirror;

namespace Bunker.Menu
{
	public class NetworkNavControls : MenuNavControls
	{
		[SerializeField]
		private NetworkManager m_Manager = null;
		[SerializeField]
        private MultiInputButton m_DisconnectButton = null;
		[SerializeField]
        private MultiInputButton m_HostButton = null;
		[SerializeField]
        private MultiInputButton m_JoinButton = null;
		
		public override void Show()
        {
            base.Show();
            if (m_Manager != null)
            {
				if(m_DisconnectButton != null){
                	m_DisconnectButton.interactable = (m_Manager.isNetworkActive);
                	m_DisconnectButton.RefreshInteractable();
				}
				if(m_HostButton != null){
                	m_HostButton.interactable = (!m_Manager.isNetworkActive);
                	m_HostButton.RefreshInteractable();
				}
				if(m_JoinButton != null){
                	m_JoinButton.interactable = (!m_Manager.isNetworkActive);
                	m_JoinButton.RefreshInteractable();
				}
            }
        }

		public void OnDisconnectPress()
		{
			
		}
	}
}