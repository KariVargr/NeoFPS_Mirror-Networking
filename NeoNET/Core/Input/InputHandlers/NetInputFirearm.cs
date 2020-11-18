using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.Constants;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;
using NeoFPS.ModularFirearms;

namespace NeoFPS.Mirror
{
	[HelpURL("https://docs.neofps.com/manual/inputref-mb-inputfirearm.html")]
	[RequireComponent (typeof (IModularFirearm))]
	public class NetInputFirearm : InputFirearm
    {

		private IModularFirearm m_Firearm = null;
        private CharacterInstance m_NetworkInstance;
		
		protected override void OnAwake()
		{
			m_Firearm = GetComponent<IModularFirearm>();

            base.OnAwake();
		}

        protected override void OnEnable()
        {
            base.OnEnable();
			if(m_Firearm.wielder != null)
				m_NetworkInstance = (m_Firearm.wielder as NetworkPlayer.FpsNetCharacter).networkInstance;
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