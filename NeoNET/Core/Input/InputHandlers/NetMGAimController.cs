using UnityEngine;
using Mirror;
using NeoFPS;
using NeoFPS.Constants;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPS.Mirror
{
    [HelpURL("https://docs.neofps.com/manual/inputref-mb-mouseandgamepadaimcontroller.html")]
	public class NetMGAimController : MouseAndGamepadAimController
    {
        private CharacterInstance m_NetworkInstance;
        protected override void Awake()
        {
            if(m_NetworkInstance == null)
                m_NetworkInstance = GetComponent<CharacterInstance>();

            base.Awake();
        }

        protected override void LateUpdate ()
        {
            if(m_NetworkInstance == null || !m_NetworkInstance.hasAuthority)
				return;

			base.LateUpdate();
        }
    }
}