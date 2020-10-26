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
		[SerializeField] // Temp to See who owns from editor
		protected ClientInstance m_ClientInstance;
		protected NeoCharacterController m_Controller;
		protected MotionController m_MotionController;
		//protected MotionController m_AnimationController;

		private IWieldable m_ActiveWeapon;
		private IQuickSlots m_WeaponInventory;
		private IHealthManager m_Health;

		[SerializeField]
		private bool m_serverInputAuth;
		public ClientInstance Owner
		{
			get{ return m_ClientInstance; }
			private set{ m_ClientInstance = value; }
		}

		public bool ServerInputAuth
		{
			get{ return m_serverInputAuth; }
			private set{ m_serverInputAuth = value; }
		}

		private void Awake()
		{
			this.enabled = false;
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
				m_ClientInstance = ClientInstance.ReturnClientInstance(base.connectionToClient);
				//AnimationHandler

				//Stanima
				IStaminaSystem stamina = GetComponent<IStaminaSystem>();
				if(stamina != null)
					stamina.onStaminaChanged += OnStaminaChanged;

				//Setup Heal Watchers Calls
				GetComponent<FpsNetCharacter>().controller = m_ClientInstance.GetComponent<FpsNetPlayerController>();
				if(!ServerInputAuth && base.isServer && !base.hasAuthority)
					m_Controller.enabled = false;

				m_MotionController.onGroundImpact += GroundTest;

			}else{
				m_Controller.enabled = false;
			}

			//Weapon/Inventory Watchers to make calls easier for weapon changes
			m_WeaponInventory = GetComponent<IQuickSlots>();
			if(m_WeaponInventory != null){
				m_WeaponInventory.onSelectionChanged += OnWeaponChanged;
				//weaponSlots.onSlotItemChanged
				//weaponSlots.onItemDropped
				
				if(m_WeaponInventory is IInventory inventory){
					//inventory.onItemAdded
					//inventory.onItemRemoved
				}
				
			}
			this.enabled = true;
		}
		private void GroundTest(Vector3 inputdata)
		{
			Debug.Log(Owner.name +" - "+inputdata);
		}
		#region Stamina
		protected virtual void OnStaminaChanged()
		{
			//if(base.isServer)
				//SendStamina();
		}
		#endregion
		#region Weapons
		protected virtual void OnWeaponChanged(IQuickSlotItem target)
		{
			
			Debug.Log(target.transform.name);
			if(base.hasAuthority)
				CmdWeaponChange(target.quickSlot);

			if(m_ActiveWeapon != null)
			{   
				/*if(m_ActiveWeapon is IThrownWeapon thrownWeapon)
				{
					thrownWeapon.onThrowLight -= UseMainAttack;
					thrownWeapon.onThrowHeavy -= UseAltAttack;
				}*/

				//if(m_ActiveWeapon is IMeleeWeapon meleeWeapon)
				//    meleeWeapon.onAttack -= UseMainAttack;

				if(m_ActiveWeapon is ModularFirearms.IModularFirearm fireWeapon)
				{
					fireWeapon.onModeChange -= OnModeChange;

					if(fireWeapon.shooter is ModularFirearms.NetShooterBehaviour shooter)
						shooter.onNetShoot -= UseMainAttack;

					if(fireWeapon.ammo != null)
					{
						if(m_serverInputAuth && base.isServer && !base.hasAuthority)
							fireWeapon.ammo.onCurrentAmmoChange -= AmmoSync;

						if(base.hasAuthority && !base.isServer)
							fireWeapon.ammo.onCurrentAmmoChange -= AmmoSync;
					}
					// Look Over Reload again
					if(fireWeapon.reloader != null)
					{
						if(base.isClientOnly){
							if(m_serverInputAuth){
								fireWeapon.reloader.onReloadStart -= onReloadStart;
							}else{
								fireWeapon.reloader.onCurrentMagazineChange -= onMagChange;
							}
						}

						if(base.isServer && !base.hasAuthority && m_serverInputAuth)
							fireWeapon.reloader.onCurrentMagazineChange -= onMagChange;
					}
				}
			}
			m_ActiveWeapon = target.wieldable;
			if(m_ActiveWeapon != null)
			{
				if(base.hasAuthority)
					m_ActiveWeapon.GetComponent<FpsInput>().enabled = true;
				else
					m_ActiveWeapon.GetComponent<FpsInput>().enabled = false;
					
				/*if(m_ActiveWeapon is IThrownWeapon thrownWeapon)
				{
					thrownWeapon.onThrowLight += UseAltAttack;
					thrownWeapon.onThrowHeavy += UseMainAttack;
				}*/

				//if(m_ActiveWeapon is IMeleeWeapon meleeWeapon)
					//meleeWeapon.onAttack += UseMainAttack;

				if(m_ActiveWeapon is ModularFirearms.IModularFirearm fireWeapon)
				{
					fireWeapon.onModeChange += OnModeChange;

					if(fireWeapon.shooter is ModularFirearms.NetShooterBehaviour shooter)
						shooter.onNetShoot += UseMainAttack;

					if(fireWeapon.ammo != null)
					{
						if(m_serverInputAuth && base.isServer && !base.hasAuthority)
							fireWeapon.ammo.onCurrentAmmoChange += AmmoSync;

						if(base.hasAuthority && !base.isServer)
							fireWeapon.ammo.onCurrentAmmoChange += AmmoSync;
					}
					// Look Over Reload again
					if(fireWeapon.reloader != null)
					{
						if(base.isClientOnly){
							if(m_serverInputAuth){
								fireWeapon.reloader.onReloadStart += onReloadStart;
							}else{
								fireWeapon.reloader.onCurrentMagazineChange += onMagChange;
							}
						}

						if(base.isServer && !base.hasAuthority && m_serverInputAuth)
							fireWeapon.reloader.onCurrentMagazineChange += onMagChange;
					}
				}
			}
		}   
		[Command]
		private void CmdWeaponChange(int target)
		{
			RpcWeaponChange(target);
			if (base.isServerOnly || (base.isClient && !base.hasAuthority))
				m_WeaponInventory.SelectSlot(target);
		}
		[ClientRpc]
		private void RpcWeaponChange(int target)
		{
			
			if (base.hasAuthority || base.isServer)
				return;

			m_WeaponInventory.SelectSlot(target);
		}

		private void OnModeChange(ModularFirearms.IModularFirearm firearm, string name)
		{
			if(base.hasAuthority)
				CmdModeChange(name);
		}
		[Command]
		private void CmdModeChange(string name)
		{
			RpcModeChange(name);
			if (base.isServerOnly || (base.isClient && !base.hasAuthority)){
				/*if ((m_ActiveWeapon as ModularFirearms.ModularFirearm).modeSwitcher != null)
					(m_ActiveWeapon as ModularFirearms.ModularFirearm).modeSwitcher.SwitchModeTo(name);
				else*/
					(m_ActiveWeapon as ModularFirearms.ModularFirearm).mode = name;
			}
		}
		[ClientRpc]
		private void RpcModeChange(string name)
		{
			if (base.hasAuthority || base.isServer)
				return;

			/*if ((m_ActiveWeapon as ModularFirearms.ModularFirearm).modeSwitcher != null)
				(m_ActiveWeapon as ModularFirearms.ModularFirearm).modeSwitcher.SwitchModeTo(name);
			else*/
				(m_ActiveWeapon as ModularFirearms.ModularFirearm).mode = name;
		}
		
		#region Ammo Monitor
		private void AmmoSync(ModularFirearms.IModularFirearm weapon, int amount)
		{
			if(base.isServer){
				TargetAmmoSync(amount);
			}else{
				CmdAmmoSync(amount);
			}
		}
		[Command]
		private void CmdAmmoSync(int amount)
		{
			int oldAmmoCount = (m_ActiveWeapon as ModularFirearms.ModularFirearm).ammo.currentAmmo;
			if(oldAmmoCount > amount){
				(m_ActiveWeapon as ModularFirearms.ModularFirearm).ammo.DecrementAmmo(oldAmmoCount - amount);
			}else if(oldAmmoCount < amount){
				(m_ActiveWeapon as ModularFirearms.ModularFirearm).ammo.IncrementAmmo(amount - oldAmmoCount);
			}
		}
		[TargetRpc]
		private void TargetAmmoSync(int amount)
		{
			int oldAmmoCount = (m_ActiveWeapon as ModularFirearms.ModularFirearm).ammo.currentAmmo;
			if(oldAmmoCount > amount){
				(m_ActiveWeapon as ModularFirearms.ModularFirearm).ammo.DecrementAmmo(oldAmmoCount - amount);
			}else if(oldAmmoCount < amount){
				(m_ActiveWeapon as ModularFirearms.ModularFirearm).ammo.IncrementAmmo(amount - oldAmmoCount);
			}
		}
		#endregion
		private void onReloadStart(ModularFirearms.IModularFirearm weapon)
		{
			if(base.hasAuthority && m_serverInputAuth && !base.isServer)
				CmdReload(0);
		}
		private void onMagChange(ModularFirearms.IModularFirearm weapon, int amount)
		{
			if(base.isServer && m_serverInputAuth)
				TargetReload(amount);
			
			if(base.isClientOnly && !m_serverInputAuth)
				CmdReload(amount);
			
		}

		[Command]
		private void CmdReload(int amount)
		{
			if (base.isServerOnly || (base.isClient && !base.hasAuthority)){
				if(m_serverInputAuth){
					(m_ActiveWeapon as ModularFirearms.ModularFirearm).reloader.Reload();
				}else{
					int oldMagazineSize = (m_ActiveWeapon as ModularFirearms.ModularFirearm).reloader.currentMagazine;
					(m_ActiveWeapon as ModularFirearms.ModularFirearm).ammo.DecrementAmmo (amount - oldMagazineSize);
					(m_ActiveWeapon as ModularFirearms.ModularFirearm).reloader.currentMagazine = amount;
				}
			}
		}
		[TargetRpc]
		public void TargetReload(int amount)
		{
			if(m_serverInputAuth){
				(m_ActiveWeapon as ModularFirearms.ModularFirearm).reloader.ManualReloadComplete(); // Write Overrider here to block ammo change this will be sent from server
				(m_ActiveWeapon as ModularFirearms.ModularFirearm).reloader.currentMagazine = amount;
			}
		}

		#region Attack Calls  
		private void UseMainAttack(Vector3 position, Vector3 forward)
		{
			Debug.Log(this.name +" MAIN FIRE");
			if (base.hasAuthority){
				double adjustedNetworkTime = NetworkTime.time - (NetworkTime.rtt / 2d);
				if (base.isServer){
					RpcUseWeapon(adjustedNetworkTime, position, forward, false);
				}else{
					CmdUseWeapon(adjustedNetworkTime, position, forward, false);
				}
			}
		}

		private void UseAltAttack(Vector3 position, Vector3 forward)
		{
			Debug.Log(this.name +" MAIN FIRE");
			if (base.hasAuthority){
				double adjustedNetworkTime = NetworkTime.time - (NetworkTime.rtt / 2d);
				if (base.isServer){
					RpcUseWeapon(adjustedNetworkTime, position, forward, true);
				}else{
					CmdUseWeapon(adjustedNetworkTime, position, forward, true);
				}
			}
		}

		[Command]
		private void CmdUseWeapon (double networkTime, Vector3 position, Vector3 forward, bool altAttack)
		{
			RpcUseWeapon(networkTime, position, forward, altAttack);
			if (base.isServerOnly || (base.isClient && !base.hasAuthority))
			{
				//If player is too far from fire point on server then do not fire.
				float maxDistance = 1f;
				float distance = Vector3.Distance(position, new Vector3(transform.position.x, position.y, transform.position.z));
				if (distance >= maxDistance)
					return;

				float rollbackTime = (float)(NetworkTime.time - networkTime) - Time.fixedDeltaTime;

				if(m_ActiveWeapon is IThrownWeapon thrownWeapon){
					if(altAttack)
						thrownWeapon.ThrowLight();
					else
						thrownWeapon.ThrowHeavy();
		
				}

				if(m_ActiveWeapon is IMeleeWeapon meleeWeapon){
					if(altAttack)
						Debug.Log("Melee Currently do not Support Alt Attack");
					else
						meleeWeapon.Attack();

				}
				if(m_ActiveWeapon is ModularFirearms.IModularFirearm fireWeapon)
					Shoot(rollbackTime, position, forward, fireWeapon);
			}
				
		}
		[ClientRpc]
		private void RpcUseWeapon (double networkTime, Vector3 position, Vector3 forward, bool altAttack)
		{
			if (base.hasAuthority || base.isServer)
				return;

			//If player is too far from fire point on server then do not fire.
			float maxDistance = 1f;
			float distance = Vector3.Distance(position, new Vector3(transform.position.x, position.y, transform.position.z));
			if (distance >= maxDistance)
				return;

			float rollbackTime = (float)(NetworkTime.time - networkTime) - Time.fixedDeltaTime;

			if(m_ActiveWeapon is IThrownWeapon thrownWeapon){
				if(altAttack)
					thrownWeapon.ThrowLight();
				else
					thrownWeapon.ThrowHeavy();
	
			}

			if(m_ActiveWeapon is IMeleeWeapon meleeWeapon){
				if(altAttack)
					Debug.Log("Melee Currently do not Support Alt Attack");
				else
					meleeWeapon.Attack();

			}
			
			if(m_ActiveWeapon is ModularFirearms.IModularFirearm fireWeapon){
				if(altAttack)
					Debug.Log("Firearms Currently do not Support Alt Attack");
				else
					Shoot(rollbackTime, position, forward, fireWeapon);
				
			}

		}

		private void Shoot(float rollbackTime, Vector3 position, Vector3 forward, ModularFirearms.IModularFirearm fireWeapon) 
		{
			Debug.Log("FIRE WEAPON");
			// Shoot
			if (fireWeapon.shooter != null && fireWeapon.shooter is ModularFirearms.NetShooterBehaviour netShooter)
				netShooter.NetShoot(fireWeapon.ammo.effect, rollbackTime, position, forward);

			// Play animation *Currently not needed for Net as its Trigger only?*
			//if (m_FireAnimTriggerHash != -1 && animator != null && animator.isActiveAndEnabled)
			//	animator.SetTrigger (m_FireAnimTriggerHash);

			// Handle recoil
			if (fireWeapon.recoilHandler != null)
				fireWeapon.recoilHandler.Recoil ();

			// Show the muzzle effect & play firing sound
			if (fireWeapon.muzzleEffect != null)
				fireWeapon.muzzleEffect.Fire ();

			// Eject shell
			if (fireWeapon.ejector != null && fireWeapon.ejector.ejectOnFire)
				fireWeapon.ejector.Eject ();

			// Decrement ammo
			if(base.isServer)
				fireWeapon.reloader.DecrementMag (1);
		}
		#endregion
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

---Health---
Add Area of Damage
-Status (Alive/Dead)
-Health Changes with Source

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