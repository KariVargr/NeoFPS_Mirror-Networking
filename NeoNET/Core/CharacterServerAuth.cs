using Mirror;
using System;
using UnityEngine;
using NeoCC;
using NeoFPS.CharacterMotion;
using NeoFPS.Mirror.NetworkPlayer;

/*
Changes to Try.

Send the MotionGraph Data as we get them to the Clients

Send the Pitch Yaw and the Rotation Data

Send movement Data As Move Direction

Send World Posistion for Hack Check
*/

namespace NeoFPS.Mirror
{
    public class CharacterServerAuth : CharacterInstance
    {
        private enum ActionCodeProcessingTypes : byte
        {
            Ignore = 1,
            ClientActual = 2,
            ClientReplay = 4,
            ServerAuthoritive = 8
        }

        [Tooltip("Number of past UserInputs from client to send to server. This helps ensure movement packets make it through even when dropped.")]
        [Range(0, 3f)]
        [SerializeField]
        private int m_PastInputSends = 2;
        private ClientMotionData m_ClientMotionData = new ClientMotionData();
        private ServerMotionData m_ServerMotionData = new ServerMotionData();


        /// <summary>
        /// Initializes this script for anyone with authority or if the server.
        /// </summary>
        protected override void NetworkInitialize(bool authoritiveOrServer)
        {
            m_Controller = GetComponent<NeoCharacterController>();
            m_MotionController = GetComponent<MotionController>();
            if (authoritiveOrServer)
            {
                //WeaponHandler
                m_ClientInstance = ClientInstance.ReturnClientInstance(base.connectionToClient);
                //AnimationHandler
                //Health
                //Setup Heal Watchers Calls
                GetComponent<FpsNetCharacter>().controller = m_ClientInstance.GetComponent<FpsNetPlayerController>();
            }else{
                m_Controller.enabled = false;
            }
            this.enabled = true;
        }
        /// <summary>
        /// Phsyics update step. Called before Update.
        /// </summary>
        protected virtual void FixedUpdate()
        {
            //Authoritive client.
            if (base.hasAuthority)
            {
                ProcessUserInputResults();
                SendUserInputs();
            }

            /* If server without authority. Needed to process
             * data from other clients, but not self as client host. */
            if (base.isServer && !base.hasAuthority)
                ProcessUserInput();
        }

        /// <summary>
        /// Moves using UserInput.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="actionCodeProcessing"></param>
        private void UserInputMove(UserInput input, ActionCodeProcessingTypes actionCodeProcessing)
        {
            // Send the Move to Motion Controller
            Vector2 move = input.Direction;

            float mag = Mathf.Clamp01(move.magnitude);
            if (mag > Mathf.Epsilon)
				move.Normalize();

            m_MotionController.inputMoveDirection = move;
            m_MotionController.inputMoveScale = mag;

            //processRotation(input.Rotation);
            // Send the Aim Rot to the Place
        }
        /// <summary>
        /// Sets the transforms position.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="updatePhysicsTransforms"></param>
        private void SetTransformPosition(Vector3 pos, bool updatePhysicsTransforms = true)
        {
            //transform.position = pos;
            if(pos != transform.position){
                m_Controller.Teleport(pos, Quaternion.identity);
                Debug.Log(Owner.name +" Telport");
            }

            if (updatePhysicsTransforms)
                Physics.SyncTransforms();
        }

        /// <summary>
        /// Processed user inputs.
        /// </summary>
        [Server]
        private void ProcessUserInput()
        {
            //Nothing to process.
            if (m_ServerMotionData.UserInputs.Count == 0)
                return;

            
            //Becomes true if input was processed.
            bool processed = false;
            //Process queued input.
            for (int i = 0; i < m_ServerMotionData.UserInputs.Count; i++)
            {
                /* Input time is less than or equal last processed which means it was already processed.
                 * This will occur nearly every time due to past inputs being resent; This check is placed first
                 * to save performance by skipping other checks. */
                if (m_ServerMotionData.UserInputs[i].NetworkTime <= m_ServerMotionData.LastInputNetworkTime)
                    continue;
                //Input network time is in the future of server network time. This isn't possible. Probably trying to cheat packets.
                if (m_ServerMotionData.UserInputs[i].NetworkTime > NetworkTime.time)
                    continue;

                /* Only perform movement if network time is within MAX_INPUTS_DELAY. Otherwise
                 * mark input as processed to the server sends an un-moved response. This stops the player
                 * from moving when they have a massive ping. */
                if (NetworkTime.time - m_ServerMotionData.UserInputs[i].NetworkTime < ClientInstance.MAX_INPUTS_DELAY)
                {
                    //Perform movement on user input.
                    //processRotation(m_ServerMotionData.UserInputs[i].Rotation);
                    UserInputMove(m_ServerMotionData.UserInputs[i], ActionCodeProcessingTypes.ServerAuthoritive);
                    Debug.Log(Owner.name+" - CLIENT:" + m_ServerMotionData.UserInputs[i].Position.ToString() +" - LOCAL:"+transform.position.ToString());
                    ServerUpdateAnimator(m_ServerMotionData.UserInputs[i]);
                }

                m_ServerMotionData.LastInputNetworkTime = m_ServerMotionData.UserInputs[i].NetworkTime;
                processed = true;
            }

            //If processed send input results to owner and effect data to all.
            if (processed)
            {
                UserInput lastInput = m_ServerMotionData.UserInputs[m_ServerMotionData.UserInputs.Count - 1];
                TargetUpdateServerResult(new UserInputResult(lastInput.NetworkTime, NetworkTime.time, transform.position, 0f , Vector3.zero/*_verticalVelocity, _externalForces*/));
                //Update running which will send in another method.
                //SetRunning(_isGrounded && lastInput.LocalDirection != Vector3.zero && !lastInput.ActionCodes.Contains(ActionCodes.Walking));
            }

            m_ServerMotionData.UserInputs.Clear();
        }

        private void processRotation(Vector3 input)
        {
            //m_YawTransform.rotation = Quaternion.Euler(0f, input.y, 0f);
            //m_PitchTransform.rotation = Quaternion.Euler(input.x, 0f, 0f);
            //Debug.Log(Owner.name+" -ROT "+ input);
        }

        /// <summary>
        /// Sends queued inputs to the server.
        /// </summary>
        /// <param name="inputs"></param>
        [Command(channel = 1)]
        private void CmdMoveInput(UserInput[] inputs)
        {
            //If not enabled. Can occur when player is out of health, possible a command go through after.
            if (!this.enabled)
                return;

            /* If client has been unresponsive for awhile then drop these inputs but
             * force a response.
             * This is to stop the player from teleporting by throttling their
             * connection. */
            if (m_ClientInstance.HighLatency())
            {
                //Generate an input using the last network time from client inputs enforcing no moving direction or action codes.
                Vector3 LocalRotation = Vector3.zero;

                m_ServerMotionData.UserInputs.Add(
                    new UserInput(inputs[inputs.Length - 1].NetworkTime, 
                    transform.position, 
                    LocalRotation,
                    Vector2.zero,
                    0f)
                );
            }
            //Client has been sending data reliably enough to process.
            else
            {
                m_ServerMotionData.UserInputs.AddRange(inputs);
            }

            m_ServerMotionData.LastReceivedNetworkTime = NetworkTime.time;
        }

        /// <summary>
        /// Update AnimatorController using input.
        /// </summary>
        [Server]
        private void ServerUpdateAnimator(UserInput input)
        {
            //_animatorController.SetMovementDirection(input.LocalDirection);
        }
        
        /// <summary>
        /// Processes stored input results.
        /// </summary>
        [Client]
        private void ProcessUserInputResults()
        {
            //Nothing to process.
            if (m_ClientMotionData.InputResults.Count == 0)
                return;

            /* For TCP only the last result matters since everything
             * comes in order. However if in a UDP environment a manual search
             * to find the latest result is required. UDP isn't supported yet so
             * I will always take the last result in the collection. */
            UserInputResult serverResult = m_ClientMotionData.InputResults[m_ClientMotionData.InputResults.Count - 1];

            SetTransformPosition(serverResult.Position);
            //_verticalVelocity = serverResult.VerticalVelocity;
            //_externalForces = serverResult.ExternalForces;

            int start = 0;
            /* Reapply inputs which have been performed after but not yet
             * processed by this server response. */
            foreach (UserInput userInput in m_ClientMotionData.CachedInputs)
            {
                if (userInput.NetworkTime > serverResult.ClientNetworkTime)
                {
                    //Move using cached inputs.
                    UserInputMove(userInput, ActionCodeProcessingTypes.ClientReplay); //appears to be calling twice per recv
                }
                else
                {
                    start++;
                }
            }

            /* Remove old entries. Sometimes start won't be above
             * 0 if fixed update is called multiple times in a single
             * frame. This can occur since data is sent during fixed
             * update. */
            if (start > 0)
                m_ClientMotionData.CachedInputs.RemoveRange(0, start);

            m_ClientMotionData.InputResults.Clear();
            //Sync physics so next move starts at the right spot.
            Physics.SyncTransforms();
        }

        [Client]
        public void SendUserInputs()
        {
            m_ClientMotionData.LastSentNetworkTime = NetworkTime.time;
            
            bool highLatency = m_ClientInstance.HighLatency();
            Vector3 LocalRotation = Vector3.zero;
            Vector2 localDirection = Vector2.zero; //m_InputHandler.GetUserInputs();
            float localScale = 0f;
   
            /* If not server and if ping is ridiculously high or it's been awhile since receiving data 
             * then block movement so player doesn't have an advantage. */

            if (highLatency)
            {
                localDirection = Vector2.zero;
            }
            UserInput ui = new UserInput(NetworkTime.time, 
                    transform.position, 
                    LocalRotation,
                    localDirection,
                    localScale);
            //Apply action codes and movement as client.
            UserInputMove(ui, ActionCodeProcessingTypes.ClientActual);

            //If not client host send to server.
            if (!m_ClientHost)
            {
                 //Cache user input then send.
                m_ClientMotionData.CachedInputs.Add(ui);

                //Only send at most up to cached inputs count.
                int targetArraySize = Mathf.Min(m_ClientMotionData.CachedInputs.Count, 1 + m_PastInputSends);
                //Resize array to accomodate 
                UserInput[] inputsToSend = new UserInput[targetArraySize];

                /* Start at the end of cached inputs, and add to the end of inputs to send.
                 * This will add the older inputs first. */
                for (int i = 0; i < targetArraySize; i++)
                {
                    //Add from the end.
                    inputsToSend[targetArraySize - 1 - i] = m_ClientMotionData.CachedInputs[m_ClientMotionData.CachedInputs.Count - 1 - i];
                }
                CmdMoveInput(inputsToSend);
            }
        }

        /// <summary>
        /// Sent to owner after server has processed user input.
        /// </summary>
        /// <param name="serverResult"></param>
        [TargetRpc(channel = 1)]
        private void TargetUpdateServerResult(UserInputResult serverResult)
        {
            //Only update input results if owner. Should always be the case.
            if (base.hasAuthority)
            {
                m_ClientMotionData.LastReceivedNetworkTime = NetworkTime.time;

                //If not enabled. Can occur when player is out of health, possible a command go through after.
                if (!this.enabled)
                    return;

                m_ClientMotionData.InputResults.Add(serverResult);
                return;
            }
        }
    }
}