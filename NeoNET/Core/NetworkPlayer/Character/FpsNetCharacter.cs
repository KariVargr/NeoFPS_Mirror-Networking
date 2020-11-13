using System;
using UnityEngine;
using UnityEngine.Events;
using NeoCC;
using NeoFPS.Constants;
using NeoFPS.CharacterMotion;

namespace NeoFPS.Mirror.NetworkPlayer
{
    //Based off FpsSoloCharacter
    [HelpURL("https://docs.neofps.com/manual/fpcharactersref-mb-fpssolocharacter.html")]
    public class FpsNetCharacter : BaseCharacter
    {
        public static event UnityAction<FpsNetCharacter> onLocalPlayerCharacterChange;
        private static FpsNetCharacter m_LocalPlayerCharacter = null;
        public static FpsNetCharacter localPlayerCharacter
        {
            get { return m_LocalPlayerCharacter; }
            set
            {
                if(value == null && !m_LocalPlayerCharacter.controller.isLocalPlayer)
                    m_LocalPlayerCharacter = null;
                
                if(value.controller.isLocalPlayer){
                    m_LocalPlayerCharacter = value;
                    if (onLocalPlayerCharacterChange != null)
                        onLocalPlayerCharacterChange(m_LocalPlayerCharacter);
                }
            }
        }
    
        private CharacterInstance m_NetworkInstance = null;
        public CharacterInstance networkInstance
        {
            get { return m_NetworkInstance; }
            private set
            {
                m_NetworkInstance = value;
            }
        }
        public override bool isLocalPlayerControlled
        {
            get { return isPlayerControlled && controller.isLocalPlayer; }
        }

        public override bool isRemotePlayerControlled
        {
            get { return isPlayerControlled && !controller.isLocalPlayer; }
        }
        
        protected override void Awake() {
            if (networkInstance == null)
                networkInstance = GetComponent<CharacterInstance>();

            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
        }

        protected override void OnControllerChanged()
        {
            base.OnControllerChanged();
            
            if (controller != null)
            {
                if (controller.isLocalPlayer)
                {
                    Debug.Log(this.name + " - is Localplayer");
                    localPlayerCharacter = this;
                    SetFirstPerson(true);
                }
                else
                {
                    if (localPlayerCharacter == this)
                        localPlayerCharacter = null;
                }

                if ((FpsNetCharacter)controller.currentCharacter != this)
                    controller.currentCharacter = this;

                gameObject.SetActive(((MonoBehaviour)controller).isActiveAndEnabled);
            }
            else
            {
                
                if (localPlayerCharacter == this)
                    localPlayerCharacter = null;
                
                if(networkInstance.isServer && !healthManager.isAlive){
                    Debug.Log("Remove the Old Char");
                    Destroy(this.gameObject);
                }
                // Disable the object (needs a controller to function)
                gameObject.SetActive(false);
            }
        }
    }
}

