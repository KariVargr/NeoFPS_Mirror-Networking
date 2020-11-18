using Mirror;
using System;
using UnityEngine;
using NeoCC;
using NeoFPS.CharacterMotion;
using NeoFPS.Mirror.NetworkPlayer;

namespace NeoFPS.Mirror
{
/* ---------------------------------------------
    Notes:
    Build Thrown Weapon Hooks
    Build Melee Weapon Hooks
    Review Fire Weapon Hooks
    Fix Mode Change
    Review Ammo and Reload
----------------------------------------------*/
    
    public class NetWeaponHandler : NetworkBehaviour
	{
		[SerializeField]
        private IQuickSlots m_WeaponInventory;
        private IWieldable m_ActiveWeapon;

        private bool isServerAuthoritative
        {
            get
            { 
                if(FpsGameMode.current is FpsNetGameMinimal gameMode)    
                    return gameMode.ServerAuthoritative;
                
                return false;
            }
        }

		public override bool OnSerialize(NetworkWriter writer, bool initialState)
		{
			if(initialState)
			{
				//writer.WriteNetworkIdentity((m_Character.controller as FpsNetPlayerController).GetComponent<NetworkIdentity>());
			}
			return base.OnSerialize(writer, initialState);
		}

		public override void OnDeserialize(NetworkReader reader, bool initialState)
		{
			if(initialState)
			{	
				//m_Character.controller = reader.ReadNetworkIdentity().GetComponent<IController>();
			}
			base.OnDeserialize(reader, initialState);
		}

        private void Awake() {
            if(m_WeaponInventory == null)
                m_WeaponInventory = GetComponent<IQuickSlots>();

            //Setup the Weapon Watcher / Hooks
            m_WeaponInventory.onSelectionChanged += OnWeaponChanged;
            //weaponSlots.onSlotItemChanged
			//weaponSlots.onItemDropped
			
            // Maybe remove Inventory Stuff??
			if(m_WeaponInventory is IInventory inventory){
			    //inventory.onItemAdded
				//inventory.onItemRemoved
			}
        }

		protected void OnWeaponChanged(IQuickSlotItem target)
		{
			if(base.hasAuthority && !isServerAuthoritative)
				CmdWeaponChange(target.quickSlot);

			if(m_ActiveWeapon != null)
			{   
				if(m_ActiveWeapon is IThrownWeapon thrownWeapon)
                    OnThrownChange(thrownWeapon);

				if(m_ActiveWeapon is IMeleeWeapon meleeWeapon)
				    OnMeleeChange(meleeWeapon);

				if(m_ActiveWeapon is ModularFirearms.IModularFirearm fireWeapon)
                    OnFirearmChange(fireWeapon);
			}

			m_ActiveWeapon = target.wieldable;

			if(m_ActiveWeapon != null)
			{
				if(base.hasAuthority)
					m_ActiveWeapon.GetComponent<FpsInput>().enabled = true;
				else
					m_ActiveWeapon.GetComponent<FpsInput>().enabled = false;

                if(m_ActiveWeapon is IThrownWeapon thrownWeapon)
                    OnThrownChange(thrownWeapon, true);

				if(m_ActiveWeapon is IMeleeWeapon meleeWeapon)
				    OnMeleeChange(meleeWeapon, true);

				if(m_ActiveWeapon is ModularFirearms.IModularFirearm fireWeapon)
                    OnFirearmChange(fireWeapon, true);
			}
		}
        protected virtual void OnMeleeChange(IMeleeWeapon weapon, bool set = false)
        {
            if(!set)
            {

            }
            else
            {

            }
        }
        protected virtual void OnThrownChange(IThrownWeapon weapon, bool set = false)
        {
            if(!set)
            {

            }
            else
            {
                
            }
        }
        protected virtual void OnFirearmChange(ModularFirearms.IModularFirearm weapon, bool set = false)
        {
            // Review and Fix
            if(!set) // Clearing the Watchers
            {
                weapon.onModeChange -= OnModeChange;
				if(weapon.shooter is ModularFirearms.NetShooterBehaviour shooter)
					shooter.onNetShoot -= UseMainAttack;

				if(weapon.ammo != null)
				{
					if(isServerAuthoritative && base.isServer && !base.hasAuthority)
						weapon.ammo.onCurrentAmmoChange -= AmmoSync;

					if(base.hasAuthority && !base.isServer)
						weapon.ammo.onCurrentAmmoChange -= AmmoSync;
				}
				// Look Over Reload again
				if(weapon.reloader != null)
				{
					if(base.isClientOnly){
						if(isServerAuthoritative){
							weapon.reloader.onReloadStart -= onReloadStart;
						}else{
							weapon.reloader.onCurrentMagazineChange -= onMagChange;
						}
					}

					if(base.isServer && !base.hasAuthority && isServerAuthoritative)
						weapon.reloader.onCurrentMagazineChange -= onMagChange;
				}
            }
            else // Setting the Watchers
            {
                weapon.onModeChange += OnModeChange;
				if(weapon.shooter is ModularFirearms.NetShooterBehaviour shooter)
					shooter.onNetShoot += UseMainAttack;

				if(weapon.ammo != null)
				{
					if(isServerAuthoritative && base.isServer && !base.hasAuthority)
						weapon.ammo.onCurrentAmmoChange += AmmoSync;

					if(base.hasAuthority && !base.isServer)
						weapon.ammo.onCurrentAmmoChange += AmmoSync;
				}
				// Look Over Reload again
				if(weapon.reloader != null)
				{
					if(base.isClientOnly){
						if(isServerAuthoritative){
							weapon.reloader.onReloadStart += onReloadStart;
						}else{
							weapon.reloader.onCurrentMagazineChange += onMagChange;
						}
					}

					if(base.isServer && !base.hasAuthority && isServerAuthoritative)
						weapon.reloader.onCurrentMagazineChange += onMagChange;
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
			if ((base.hasAuthority && !isServerAuthoritative) || base.isServer)
				return;

			m_WeaponInventory.SelectSlot(target);
		}

		private void OnModeChange(ModularFirearms.IModularFirearm firearm, string name)
		{
            // Note: Look as adding Current inventory slot or ID to check if its the same weapon on both Client and Server??

			if(base.hasAuthority && !isServerAuthoritative)
				CmdModeChange(name);
		}
		[Command]
		private void CmdModeChange(string name)
		{
			// Note: Need to fix for Update Mode Change.
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
			if ((base.hasAuthority && !isServerAuthoritative) || base.isServer)
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
			if(base.hasAuthority && isServerAuthoritative && !base.isServer)
				CmdReload(0);
		}
		private void onMagChange(ModularFirearms.IModularFirearm weapon, int amount)
		{
			if(base.isServer && isServerAuthoritative)
				TargetReload(amount);
			
			if(base.isClientOnly && !isServerAuthoritative)
				CmdReload(amount);
			
		}

		[Command]
		private void CmdReload(int amount)
		{
			if (base.isServerOnly || (base.isClient && !base.hasAuthority)){
				if(isServerAuthoritative){
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
			if(isServerAuthoritative){
				(m_ActiveWeapon as ModularFirearms.ModularFirearm).reloader.ManualReloadComplete(); // Write Overrider here to block ammo change this will be sent from server
				(m_ActiveWeapon as ModularFirearms.ModularFirearm).reloader.currentMagazine = amount;
			}
		}

#if FIRSTGEARGAMES_COLLIDERROLLBACKS
#region FGG's Lag Comp / RollBack
		protected virtual void UseMainAttack(Vector3 position, Vector3 forward)
		{
			if (base.hasAuthority && !isServerAuthoritative){
				uint fixedTime = FixedTimeSync.FixedFrame;
				if (base.isServer){
					RpcUseWeapon(fixedTime, position, forward, false);
				}else{
					CmdUseWeapon(fixedTime, position, forward, false);
				}
			}
		}

		protected virtual void UseAltAttack(Vector3 position, Vector3 forward)
		{
			if (base.hasAuthority && !isServerAuthoritative){
				uint fixedTime = FixedTimeSync.FixedFrame;
				if (base.isServer){
					RpcUseWeapon(fixedTime, position, forward, true);
				}else{
					CmdUseWeapon(fixedTime, position, forward, true);
				}
			}
		}
		[Command]
		private void CmdUseWeapon (uint fixedTime, Vector3 position, Vector3 forward, bool altAttack)
		{
			RpcUseWeapon(fixedTime, position, forward, altAttack);
			if (base.isServerOnly || (base.isClient && !base.hasAuthority))
				UseWeaponHandler(FixedTimeSync.FixedFrame - (fixedTime - FixedTimeSync.InterpolationReduction), position, forward, altAttack);
			
		}
		[ClientRpc] // Temp change to fixed time
		private void RpcUseWeapon (uint fixedTime, Vector3 position, Vector3 forward, bool altAttack)
		{
			if ((base.hasAuthority && !isServerAuthoritative) || base.isServer)
				return;
			UseWeaponHandler(FixedTimeSync.FixedFrame - (fixedTime - FixedTimeSync.InterpolationReduction), position, forward, altAttack);
		}
#endregion
#else
#region MLAPI Based Lag Comp / Rollback
		protected virtual void UseMainAttack(Vector3 position, Vector3 forward)
		{
			Debug.Log(this.name +" MAIN FIRE");
			if (base.hasAuthority && !isServerAuthoritative){
				if (base.isServer){
					RpcUseWeapon(NetworkTime.time, position, forward, false);
				}else{
					CmdUseWeapon(NetworkTime.time, position, forward, false);
				}
			}
		}

		protected virtual void UseAltAttack(Vector3 position, Vector3 forward)
		{
			Debug.Log(this.name +" ALT FIRE");
			if (base.hasAuthority && !isServerAuthoritative){
				if (base.isServer){
					RpcUseWeapon(NetworkTime.time, position, forward, true);
				}else{
					CmdUseWeapon(NetworkTime.time, position, forward, true);
				}
			}
		}
		[Command]
		private void CmdUseWeapon (double fixedTime, Vector3 position, Vector3 forward, bool altAttack)
		{
			RpcUseWeapon(fixedTime, position, forward, altAttack);
			if (base.isServerOnly || (base.isClient && !base.hasAuthority))
				UseWeaponHandler((float)(NetworkTime.time - fixedTime), position, forward, altAttack);
			
		}
		[ClientRpc] // Temp change to fixed time
		private void RpcUseWeapon (double fixedTime, Vector3 position, Vector3 forward, bool altAttack)
		{
			if ((base.hasAuthority && !isServerAuthoritative) || base.isServer)
				return;

			UseWeaponHandler((float)(NetworkTime.time - fixedTime), position, forward, altAttack);
		}
#endregion
#endif
        private void UseWeaponHandler(float timeDelay, Vector3 position, Vector3 forward, bool altAttack)
        {
			//Things to Add?
			//Weapon Check
			//Ammo Check
			//Delay Check
				
			//If player is too far from fire point on server then do not fire.
			float maxDistance = 1f;
			float distance = Vector3.Distance(position, new Vector3(transform.position.x, position.y, transform.position.z));
			if (distance >= maxDistance)
				return;

			// Untested Thrown and Melee
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
					Debug.Log("Firearm Currently do not Support Alt Attack");
				else
                    Shoot(timeDelay, position, forward, fireWeapon);
            }
        }
		// Cloned from ModularFirearm.cs line:480
		private void Shoot(float timeDelay, Vector3 position, Vector3 forward, ModularFirearms.IModularFirearm fireWeapon) 
		{
			Debug.Log("Shoot - "+timeDelay+" - "+NetworkTime.rtt);
			// Shoot
			if (fireWeapon.shooter != null && fireWeapon.shooter is ModularFirearms.NetShooterBehaviour netShooter)
				netShooter.NetShoot(fireWeapon.ammo.effect, timeDelay, position, forward);

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
		/*
		private void Shoot ()
		{
			if (reloader != null && reloader.empty)
			{
				if (Reload () == false)
                    PlayDryFireSound ();
                return;
            }

            if (reloading && reloader.interruptable)
            {
                reloader.Interrupt();
                return;
            }

			// Shoot
			if (shooter != null)
				shooter.Shoot (currentAccuracy * moveAccuracyModifier * currentAimerAccuracy, ammo.effect);

			// Play animation
			if (m_FireAnimTriggerHash != -1 && animator != null && animator.isActiveAndEnabled)
				animator.SetTrigger (m_FireAnimTriggerHash);

			// Handle recoil
            if (recoilHandler != null)
			    recoilHandler.Recoil ();

			// Show the muzzle effect & play firing sound
			if (muzzleEffect != null)
				muzzleEffect.Fire ();

			// Eject shell
			if (ejector != null && ejector.ejectOnFire)
				ejector.Eject ();

            // Decrease the accuracy
            if (recoilHandler != null)
            {
                if (aimToggleHold.on)
                    currentAccuracy -= recoilHandler.sightedAccuracyKick;
                else
                    currentAccuracy -= recoilHandler.hipAccuracyKick;
            }

			// Decrement ammo
			reloader.DecrementMag (1);
		}*/
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