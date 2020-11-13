using Mirror;
using System;
using UnityEngine;
using NeoFPS.Mirror.NetworkPlayer;

namespace NeoFPS.Mirror
{

    public class ClientInstance : NetworkBehaviour
    {
        #region Public.
        public static ClientInstance Instance;
        public PlayerSpawner PlayerSpawner { get; private set; }
        #endregion

        #region Private.
        private FpsNetPlayerController m_Player;
        private double _clientLastReceivedNetworkTime = -1f;
        private double _serverLastReceivedNetworkTime = -1f;
        private double _serverLastClientNetworkTime = -1f;
        [SerializeField]
        private bool _initialized = false;
        private string _errorMessage = string.Empty;
        #endregion

        #region Constants.
        public const double MAX_INPUTS_DELAY = 150d;
        private const float MAX_UNRESPONSIVE_TICKS = 10f;
        private const int VERSION_CODE = 3;
        #endregion

        private bool isServerAuthoritative
        {
            get
            { 
                if(FpsGameMode.current is FpsNetGameMinimal gameMode)    
                    return gameMode.ServerAuthoritative;
                
                return false;
            }
        }

        public override void OnStartAuthority()
        {
            base.OnStartAuthority();
            Instance = this;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
        }
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            PlayerSpawner = GetComponent<PlayerSpawner>();
            CmdVerifyVersion(VERSION_CODE);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            base.name = base.name+"-"+base.netId;
        }

        private void FixedUpdate()
        {
            if (!_initialized)
                return;
            if (base.hasAuthority)
                CmdResetHighLatency(NetworkTime.time);
        }

        public bool HighLatency()
        {
            //If client only.
            if (base.isClientOnly)
            {
                bool isHigh = ((NetworkTime.rtt * 1000f) > (MAX_INPUTS_DELAY * 2f)) ||
                    (NetworkTime.time - _clientLastReceivedNetworkTime > Time.fixedDeltaTime * MAX_UNRESPONSIVE_TICKS);

                if (isHigh)
                    _errorMessage = "Your connection is unreliable. Please upgrade your potato.";
                else
                    _errorMessage = string.Empty;

                return isHigh;
            }
            //If server
            else if (base.isServerOnly)
            {
                //If haven't heard from client in awhile.
                return (NetworkTime.time - _serverLastClientNetworkTime > MAX_INPUTS_DELAY) ||
                    (NetworkTime.time - _serverLastReceivedNetworkTime > Time.fixedDeltaTime * MAX_UNRESPONSIVE_TICKS);
            }
            //If client host, never have high latency.
            else
            {
                return false;
            }
        }

        public static ClientInstance ReturnClientInstance(NetworkConnection conn)
        {
            /* Check if server only. This is how it has to be done because Mirror
             * doesn't have any way to properly tell if server, client, or client host.
             * Dumb Mirror. */
            if (NetworkServer.active)// && !NetworkClient.active)
            {
                NetworkIdentity localPlayer;
                if (NeoFPSNetworkManager.LocalPlayers.TryGetValue(conn, out localPlayer))
                    return localPlayer.GetComponent<ClientInstance>();
                else
                    return null;
            }
            //Client.
            else
            {
                return Instance;
            }
        }

        [Command(channel = 1)]
        private void CmdResetHighLatency(double clientNetworkTime)
        {
            _serverLastClientNetworkTime = clientNetworkTime;
            _serverLastReceivedNetworkTime = NetworkTime.time;
            TargetResetHighLatency();
        }

        [TargetRpc]
        private void TargetResetHighLatency()
        {
            _clientLastReceivedNetworkTime = NetworkTime.time;
        }

        [Command]
        private void CmdVerifyVersion(int versionCode)
        {
            bool pass = (versionCode == VERSION_CODE);
            TargetVerifyVersion(pass);
        }

        [TargetRpc]
        private void TargetVerifyVersion(bool pass)
        {
            if (!pass)
            {
                _errorMessage = "Your executable is out of date. Please update.";
                NetworkClient.Disconnect();
            }
            else
            {
                _initialized = true;
                GetComponent<FpsNetPlayerController>().Initialize();
            }
        }

        private void OnGUI()
        {
            if (_errorMessage == string.Empty)
                return;

            int w = Screen.width, h = Screen.height;

            GUIStyle style = new GUIStyle();

            Rect rect = new Rect(10, 150, w, h * 2 / 50);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = h * 2 / 50;
            style.normal.textColor = Color.red;
            GUI.Label(rect, _errorMessage, style);
        }
    }

}
