using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.Constants;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;
using NeoFPS.ModularFirearms;

namespace NeoFPS.Mirror
{
	[HelpURL("https://docs.neofps.com/manual/inputref-mb-inputthrownweapon.html")]
	[RequireComponent (typeof (IThrownWeapon))]
	public class NetInputThrown : InputThrownWeapon
    {

		private IThrownWeapon m_ThrownWeapon = null;
        private CharacterInstance m_NetworkInstance;
		
		protected override void OnAwake()
		{
			m_ThrownWeapon = GetComponent<IThrownWeapon>();

            base.OnAwake();
		}

        protected override void OnEnable()
        {
			m_NetworkInstance = (GetComponent<IInventoryItem>().owner as NetworkPlayer.FpsNetCharacter).networkInstance;

            base.OnEnable();
        }

		protected override void OnDisable ()
		{
			base.OnDisable();

			m_NetworkInstance = null;
		}

        protected override void Update()
        {
            if(m_NetworkInstance == null || !m_NetworkInstance.hasAuthority)
				return;
			// Look for possibly Better Solution???
            if(!m_NetworkInstance.isServerAuthoritative)
			    base.Update();
        }
	}
}