using UnityEngine;
using Mirror;
using NeoFPS;
using NeoFPS.Constants;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPS.Mirror
{
	[HelpURL("https://docs.neofps.com/manual/inputref-mb-inputcharactermotion.html")]
	[RequireComponent (typeof (ICharacter))]
	public class NetInputCharacterMotion : InputCharacterMotion
    {
        [SerializeField]
        private NetworkMotionGraph m_NetworkGraph;
        private CharacterInstance m_NetworkInstance;
        protected override void OnAwake()
        {
            if(m_NetworkInstance == null)
                m_NetworkInstance = GetComponent<CharacterInstance>();

            base.OnAwake();
        }

        protected override void UpdateInput()
        {
            if(m_NetworkInstance.isServerAuthoritative)
            {
                // Movement input get the move input in advance to sent to server
                Vector2 move = new Vector2 (
                    GetAxis (FpsInputAxis.MoveX),
                    GetAxis (FpsInputAxis.MoveY)
                );
                if (GetButton (FpsInputButton.Forward))
                    move.y += 1f;
                if (GetButton (FpsInputButton.Backward))
                    move.y -= 1f;
                if (GetButton (FpsInputButton.Left))
                    move.x -= 1f;
                if (GetButton (FpsInputButton.Right))
                    move.x += 1f;
            }

            base.UpdateInput();
        }

        //Added to Network bypass
        protected override void Update()
        {
            if(m_NetworkInstance == null || !m_NetworkInstance.hasAuthority)
				return;

            if(!m_NetworkInstance.isServerAuthoritative)
			    base.Update();
        }
    }
}