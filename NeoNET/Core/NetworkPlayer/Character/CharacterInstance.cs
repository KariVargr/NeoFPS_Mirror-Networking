using Mirror;
using System;
using UnityEngine;
using NeoCC;
using NeoFPS.CharacterMotion;
using NeoFPS.Mirror.NetworkPlayer;

namespace NeoFPS.Mirror
{
	public class CharacterInstance : NetworkBehaviour
	{
		protected bool m_ClientHost { get { return (base.isClient && base.isServer); } }
		[SerializeField]
		protected FpsNetCharacter m_Character;
		protected NeoCharacterController m_Controller;
		protected MotionController m_MotionController;
		//protected MotionController m_AnimationController;
		private NetWeaponHandler m_WeaponHandler;
		private IHealthManager m_Health;
		// Made Public for now as the Input watcher are looking at this.
		public bool isServerAuthoritative
        {
            get
            { 
                if(FpsGameMode.current is FpsNetGameMinimal gameMode)    
                    return gameMode.ServerAuthoritative;
                
                return false;
            }
        }

		private void Awake()
		{
			this.enabled = false;
		}

		public override bool OnSerialize(NetworkWriter writer, bool initialState)
		{
			if(initialState)
			{
				writer.WriteNetworkIdentity((m_Character.controller as FpsNetPlayerController).GetComponent<NetworkIdentity>());
			}
			return base.OnSerialize(writer, initialState);
		}

		public override void OnDeserialize(NetworkReader reader, bool initialState)
		{
			if(initialState)
			{	
				m_Character.controller = reader.ReadNetworkIdentity().GetComponent<IController>();
			}
			base.OnDeserialize(reader, initialState);
		}

		public override void OnStartClient()
		{
			base.OnStartClient();
			NetworkInitialize(base.hasAuthority || base.isServer);
		}
		public override void OnStartServer()
		{
			base.OnStartServer();
			NetworkInitialize(true);
		}
		protected virtual void NetworkInitialize(bool authoritiveOrServer)
		{
			m_Controller = GetComponent<NeoCharacterController>();
			m_MotionController = GetComponent<MotionController>();

			if(!base.hasAuthority)
			{
				FpsInput[] inputHandlers = GetComponents<FpsInput>();
				for(int i = 0; i < inputHandlers.Length; i++)
				{
					inputHandlers[i].enabled = false;
				}
			}

			if (authoritiveOrServer)
			{
				//WeaponHandler
				//AnimationHandler

				//Stanima
				IStaminaSystem stamina = GetComponent<IStaminaSystem>();
				if(stamina != null)
					stamina.onStaminaChanged += OnStaminaChanged;

				if(!isServerAuthoritative && base.isServer && !base.hasAuthority)
					m_Controller.enabled = false;

			}else{
				m_Controller.enabled = false;
			}

			this.enabled = true;
		}
		#region Stamina
		protected virtual void OnStaminaChanged()
		{
			//if(base.isServer)
				//SendStamina();
		}
		#endregion
	}
}
/*
DAMAGE
All Weapon Damage is controlled by Server
AmmoEffect.hit calls
Enviroment??

All Force Damage is Controlled by Client unless Server Input Auth
Enviroment??

IHealth becuase networkbehavour to allow for easier call on its actions.

To Sync List

---Weapons---
Guns
-ADS
-Reload
	-Ammo Sync
-Mode
-Melee <- No melee Currently


// Need to look at changes to the Melee
Melee
-Attack
-Attack Light
-Block

Thrown
-Attack
-Attack light

---Stamina---
No Idea look in to system

---Inventory---
-Drop Item
-Add Item
-Change Weapon in Slot

Harder things to Think on.
Send all Inventoy and Weapons info along with Health and Stamian and Active weapon to New Joined Player.

Sync MotionGraph?

*/
/*
Add to ModularFirearmModeSwitcher

public string SwitchModes(string target)
		{
			// Look for Name if none Advance by 1
			for (int i = 0; i <= m_Modes.Length; i++)
			{
				if(i == m_Modes.Length){
					if (++m_Index >= m_Modes.Length)
					m_Index -= m_Modes.Length;
				}else{
					if(m_Modes[i].descriptiveName == target){
						m_Index = i;
					}
				}
				
			}

			// Enable components
			var components = m_Modes[m_Index].components;
			for (int i = 0; i < components.Length; ++i)
			{
				if (components[i] != null)
					components[i].enabled = true;
			}

			// Fire event
			m_OnSwitchModes.Invoke();

			return m_Modes[m_Index].descriptiveName;
		}
*/