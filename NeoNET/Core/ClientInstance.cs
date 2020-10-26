
using Mirror;
using System;
using UnityEngine;
using NeoFPS.Mirror.NetworkPlayer;

namespace NeoFPS.Mirror
{

    public class ClientInstance : NetworkBehaviour
    {
        #region Public.
        /// <summary>
        /// Singleton reference to the client instance.
        /// </summary>
        public static ClientInstance Instance;
        /// <summary>
        /// PlayerSpawner on this object.
        /// </summary>
        public PlayerSpawner PlayerSpawner { get; private set; }
        #endregion

        #region Private.
        /// <summary>
        /// Last time data was received from the server.
        /// </summary>
        private double _clientLastReceivedNetworkTime = -1f;
        /// <summary>
        /// Last time data was received from the client.
        /// </summary>
        private double _serverLastReceivedNetworkTime = -1f;
        /// <summary>
        /// Last NetworkTime sent by client.
        /// </summary>
        private double _serverLastClientNetworkTime = -1f;
        /// <summary>
        /// True if initialized.
        /// </summary>
        [SerializeField]
        private bool _initialized = false;
        /// <summary>
        /// Error message to paint on the screen.
        /// </summary>
        private string _errorMessage = string.Empty;
        #endregion

        #region Constants.
        /// <summary>
        /// Maximum time between UserInput and UserInputResults which the player can have before their sending becomes limited.
        /// If a packet takes longer than this value to get to the server then it's discarded.
        /// If the server's last network time to the client is longer than this value then the clients input isn't applied.
        /// </summary>
        public const double MAX_INPUTS_DELAY = 150d;
        /// <summary>
        /// Maximum number of ticks the client or server can be unresponsive before inputs are ignored.
        /// </summary>
        private const float MAX_UNRESPONSIVE_TICKS = 10f;
        /// <summary>
        /// Version of this build.
        /// </summary>
        private const int VERSION_CODE = 3;
        #endregion

        public override void OnStartAuthority()
        {
            base.OnStartAuthority();
            Instance = this;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
        }

        /// <summary>
        /// /// Called when the local player object has been set up.
        /// </summary>
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

        /// <summary>
        /// Returns if this client has high latency.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Returns the current client instance for the connection.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Asks server to reset high latency values.
        /// </summary>
        [Command(channel = 1)]
        private void CmdResetHighLatency(double clientNetworkTime)
        {
            _serverLastClientNetworkTime = clientNetworkTime;
            _serverLastReceivedNetworkTime = NetworkTime.time;
            TargetResetHighLatency();
        }


        /// <summary>
        /// Tells the client their reset request was received.
        /// </summary>
        [TargetRpc]
        private void TargetResetHighLatency()
        {
            _clientLastReceivedNetworkTime = NetworkTime.time;
        }

        /// <summary>
        /// Verifies with the server the client version.
        /// </summary>
        /// <param name="versionCode"></param>
        [Command]
        private void CmdVerifyVersion(int versionCode)
        {
            bool pass = (versionCode == VERSION_CODE);
            TargetVerifyVersion(pass);
        }

        /// <summary>
        /// Response from server if version matches.
        /// </summary>
        /// <param name="pass"></param>
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
                GetComponent<FpsNetPlayerController>().CheckLocalPlayer();
                //PlayerSpawner.TryRespawn();
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
