using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using Mirror;

namespace NeoFPS.Mirror.LagCompensation
{
    /// <summary>
    /// The main class for controlling lag compensation
    /// </summary>
    public class LagCompensationManager : MonoBehaviour
    {
        #if !FIRSTGEARGAMES_COLLIDERROLLBACKS
        private void FixedUpdate()
        {
            if (NetworkServer.active)
                AddFrames();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void FirstInitialize()
        {
            GameObject go = new GameObject();
            go.name = "LagCompensationManager";
            go.AddComponent<LagCompensationManager>();
            DontDestroyOnLoad(go);
        }
        #endif
        /// <summary>
        /// Simulation objects
        /// </summary>
        public static readonly List<TrackedObject> SimulationObjects = new List<TrackedObject>();
        /// <summary>
        /// Simulation objects
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use SimulationObjects instead", false)]
        public static List<TrackedObject> simulationObjects => SimulationObjects;


        /// <summary>
        /// Turns time back a given amount of seconds
        /// </summary>
        /// <param name="secondsAgo">The amount of seconds</param>
        /// <param name="action">The action to invoke when time is turned back</param>
        public static void StartSimulation(float secondsAgo)
        {
            StartSimulation(secondsAgo, SimulationObjects);
        }

        /// <summary>
        /// Turns time back a given amount of second on the given objects
        /// </summary>
        /// <param name="secondsAgo">The amount of seconds</param>
        /// <param name="simulatedObjects">The object to simulate back in time</param>
        /// <param name="action">The action to invoke when time is turned back</param>
        public static void StartSimulation(float secondsAgo, IList<TrackedObject> simulatedObjects)
        {
            if (!NetworkServer.active)
                return;

            for (int i = 0; i < simulatedObjects.Count; i++)
            {
                simulatedObjects[i].ReverseTransform(secondsAgo);
            }
        }
        /// <summary>
        /// Restores time back
        /// </summary>
        public static void StopSimulation()
        {
            StopSimulation(SimulationObjects);
        }
        /// <summary>
        /// Restores time back on the given objects
        /// </summary>
        public static void StopSimulation(IList<TrackedObject> simulatedObjects)
        {
            if (!NetworkServer.active)
                return;
            

            for (int i = 0; i < simulatedObjects.Count; i++)
            {
                simulatedObjects[i].ResetStateTransform();
            }
        }

        internal static void AddFrames()
        {
            for (int i = 0; i < SimulationObjects.Count; i++)
            {
                SimulationObjects[i].AddFrame();
            }
        }
    }
}
