using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS.Mirror
{
    /// <summary>
    /// Data for authoritive movement on server.
    /// </summary>
    public class ServerMotionData
    {
        /// <summary>
        /// Network time of the last input which the server has processed.
        /// </summary>
        public double LastInputNetworkTime = -1d;
        /// <summary>
        /// Unprocessed user input from the client.
        /// </summary>
        public List<UserInput> UserInputs = new List<UserInput>();
        /// <summary>
        /// Last time data was received from the client.
        /// </summary>
        public double LastReceivedNetworkTime = -1f;
    }

    /// <summary>
    /// Data for authoritive movement on client.
    /// </summary>
    public class ClientMotionData
    {
        /// <summary>
        /// Inputs which have been sent to the server and not yet returned, or inputs in queue to be sent to the server.
        /// </summary>
        public List<UserInput> CachedInputs = new List<UserInput>();
        /// <summary>
        /// Unprocessed input results from the server.
        /// </summary>
        public List<UserInputResult> InputResults = new List<UserInputResult>();
        /// <summary>
        /// Last NetworkTime input was sent.
        /// </summary>
        public double LastSentNetworkTime = -1;
        /// <summary>
        /// Last time data was received from the server.
        /// </summary>
        public double LastReceivedNetworkTime = -1f;
    }
}