using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using NeoFPS.Mirror;

namespace NeoFPS.Mirror.NetworkPlayer
{
    [HelpURL("https://docs.neofps.com/manual/fpcharactersref-mb-fpssoloplayercontroller.html")]
    public class FpsNetPlayerController : BaseController
    {
        private ClientInstance m_NetworkInstance = null;
        public ClientInstance networkInstance
        {
            get { return m_NetworkInstance; }
            private set
            {
                m_NetworkInstance = value;
            }
        }
        public static FpsNetPlayerController localPlayer
        {
            get;
            private set;
        }

        public override bool isPlayer
		{
			get { return true; }
		}

        public override bool isLocalPlayer
		{
			get { return m_NetworkInstance.isLocalPlayer; }
		}

        void Awake()
        {
            if (m_NetworkInstance == null)
                m_NetworkInstance = this.GetComponent<ClientInstance>();
            
        }

        public void Initialize()
        {
            if (localPlayer == null && isLocalPlayer){
                localPlayer = this;

                if(FpsGameMode.current is FpsNetGameMinimal gameMode)
                    gameMode.RemotePlayerSet(this);

                Debug.Log("LOCAL IS SET");
            }
        }

        void OnDestroy()
        {
            if (localPlayer == this)
                localPlayer = null;
        }
    }
}