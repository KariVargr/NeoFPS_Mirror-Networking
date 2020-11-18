using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.Constants;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;
using NeoFPS.ModularFirearms;

namespace NeoFPS.Mirror
{
	[HelpURL("https://docs.neofps.com/manual/inputref-mb-inputmeleeweapon.html")]
	[RequireComponent (typeof (IMeleeWeapon))]
	public class NetInputMelee : InputMeleeWeapon
    {

		private IMeleeWeapon m_MeleeWeapon = null;
        private CharacterInstance m_NetworkInstance;
		
		protected override void OnAwake()
		{
			m_MeleeWeapon = GetComponent<IMeleeWeapon>();

            base.OnAwake();
		}

        protected override void OnEnable()
        {
			m_NetworkInstance = (m_MeleeWeapon.wielder as NetworkPlayer.FpsNetCharacter).networkInstance;

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

            if(!m_NetworkInstance.isServerAuthoritative)
			    base.Update();
        }
	}
}