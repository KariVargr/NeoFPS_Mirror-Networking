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
        protected override void Awake() {
            if (networkInstance == null)
                networkInstance = GetComponent<CharacterInstance>();

            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
            //We dont care if there is a Controller or Not.
            gameObject.SetActive(true);
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
                    // Disable the InputContols
                }

                if ((FpsNetCharacter)controller.currentCharacter != this)
                    controller.currentCharacter = this;
            }
            else
            {
                if (localPlayerCharacter == this)
                    localPlayerCharacter = null;
                // Disable the object (needs a controller to function)
            }
        }
    }
}

