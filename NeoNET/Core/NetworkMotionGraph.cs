using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using NeoFPS;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;

namespace Mirror
{
    /// <summary>
    /// A component to synchronize NeoFPS MotionGraph states for networked objects.
    /// </summary>
    /// <remarks>
    /// <para>The MotionGraph of game objects can be networked by this component. There are two models of authority for networked movement:</para>
    /// <para>If the object has authority on the client, then it should be animated locally on the owning client. The animation state information will be sent from the owning client to the server, then broadcast to all of the other clients. This is common for player objects.</para>
    /// <para>If the object has authority on the server, then it should be animated on the server and state information will be sent to all clients. This is common for objects not related to a specific client, such as an enemy unit.</para>
    /// <para>The NetworkAnimator synchronizes all animation parameters of the selected Animator. It does not automatically sychronize triggers. The function SetTrigger can by used by an object with authority to fire an animation trigger on other clients.</para>
    /// </remarks>
    [AddComponentMenu("Network/NetworMotionGraph")]
    [RequireComponent(typeof(NetworkIdentity))]
    public class NetworkMotionGraph : NetworkBehaviour
    {
        static readonly ILogger logger = LogFactory.GetLogger(typeof(NetworkMotionGraph));

        [Header("Authority")]
        [Tooltip("Set to true if animations come from owner client,  set to false if animations always come from server")]
        public bool clientAuthority;
         [Tooltip("True to sync to owner when using clientAuthority. This setting does nothing when not using clientAuthority.")]
        public bool syncToOwner = false;

        /// <summary>
        /// The MotionController component to synchronize.
        /// </summary>
        [Header("MotionController")]
        [Tooltip("MotionController that will have parameters synchronized")]
        public MotionController m_Motion;
        private bool m_MotionStateUpdate = false;

        // Note: not an object[] array because otherwise initialization is real annoying
        string[] namesParameters;
        int[] lastIntParameters;
        float[] lastFloatParameters;
        bool[] lastSwitchParameters;
        Vector3[] lastVectorParameters;
        Transform[] lastTransformParameters;

        // multiple layers
        float sendTimer;

        bool SendMessagesAllowed
        {
            get
            {
                if (isServer)
                {
                    if (!clientAuthority)
                        return true;

                    // This is a special case where we have client authority but we have not assigned the client who has
                    // authority over it, no animator data will be sent over the network by the server.
                    //
                    // So we check here for a connectionToClient and if it is null we will
                    // let the server send animation data until we receive an owner.
                    if (netIdentity != null && netIdentity.connectionToClient == null)
                        return true;
                }

                return (hasAuthority && clientAuthority);
            }
        }

        void Awake()
        {
            // store the animator parameters in a variable - the "Animator.parameters" getter allocates
            // a new parameter array every time it is accessed so we should avoid doing it in a loop
            /* Got to get the Parameters Sorted
            parameters = m_Motion
                .Where(par => !animator.IsParameterControlledByCurve(par.nameHash))
                .ToArray();
            */
            List<string> nameList = new List<string>();
            int FloatLength = 0;
            int IntLength = 0;
            int BoolLength = 0;
            int VectorLength = 0;
            int TransformLength = 0;

            List<MotionGraphParameter> workingList = new List<MotionGraphParameter>();
            m_Motion.motionGraph.CollectParameters(workingList);

            foreach (MotionGraphParameter parameter in workingList)
            {
                var floatP = parameter as FloatParameter;
                if (floatP != null)
                {
                    nameList.Insert(FloatLength, floatP.name);
                    FloatLength++;
                    //m_FloatProperties.Add(Animator.StringToHash(floatP.name), floatP);
                    continue;
                }

                var intP = parameter as IntParameter;
                if (intP != null)
                {
                    nameList.Insert(FloatLength+IntLength, intP.name);
                    IntLength++;
                    //m_IntProperties.Add(Animator.StringToHash(intP.name), intP);
                    continue;
                }

                var switchP = parameter as SwitchParameter;
                if (switchP != null)
                {
                    nameList.Insert(FloatLength+IntLength+BoolLength, switchP.name);
                    BoolLength++;
                    //m_SwitchProperties.Add(Animator.StringToHash(switchP.name), switchP);
                    continue;
                }

                var vectorP = parameter as VectorParameter;
                if (vectorP != null)
                {
                    nameList.Insert(FloatLength+IntLength+BoolLength+VectorLength, vectorP.name);
                    VectorLength++;
                    //m_VectorProperties.Add(Animator.StringToHash(vectorP.name), vectorP);
                    continue;
                }             

                var transformP = parameter as TransformParameter;
                if (transformP != null)
                {
                    nameList.Insert(FloatLength+IntLength+BoolLength+VectorLength+TransformLength, transformP.name);
                    TransformLength++;
                    //m_TransformProperties.Add(Animator.StringToHash(transformP.name), transformP);
                    continue;
                }
            }
            
            namesParameters = nameList.ToArray();

            lastFloatParameters = new float[FloatLength];
            lastIntParameters = new int[IntLength];
            lastSwitchParameters = new bool[BoolLength];
            lastVectorParameters = new Vector3[VectorLength];
            lastTransformParameters = new Transform[TransformLength];
            
            m_Motion.onCurrentStateChanged += StateMonitor;
        }

        void StateMonitor()
        {
            m_MotionStateUpdate = true;
        }

        void FixedUpdate()
        {
            if (!SendMessagesAllowed)
                return;

            CheckSendRate();

            if (m_MotionStateUpdate)
            {
                //Update the motionHash
                using (PooledNetworkWriter writer = NetworkWriterPool.GetWriter())
                {
                    WriteParameters(writer);
                    SendMotionMessage(m_Motion.currentState.serializationKey, writer.ToArray());
                }
            }
        }

        void CheckSendRate()
        {
            if (SendMessagesAllowed && syncInterval > 0 && sendTimer < Time.time)
            {
                sendTimer = Time.time + syncInterval;

                using (PooledNetworkWriter writer = NetworkWriterPool.GetWriter())
                {
                    if (WriteParameters(writer))
                        SendMotionParametersMessage(writer.ToArray());
                }
            }
        }

        void SendMotionMessage(int stateHash, byte[] parameters)
        {
            if (isServer)
            {
                RpcOnMotionClientMessage(stateHash, parameters);
            }
            else if (ClientScene.readyConnection != null)
            {
                CmdOnMotionServerMessage(stateHash, parameters);
            }
        }

        void SendMotionParametersMessage(byte[] parameters)
        {
            if (isServer)
            {
                RpcOnMotionParametersClientMessage(parameters);
            }
            else if (ClientScene.readyConnection != null)
            {
                CmdOnMotionParametersServerMessage(parameters);
            }
        }

        void HandleMotionMsg(int stateHash, NetworkReader reader)
        {
            if (hasAuthority && clientAuthority)
                return;

            var sentState = m_Motion.motionGraph.GetStateFromKey(stateHash);
            if(m_Motion.currentState != sentState){
                //Force Change of State
            }

            ReadParameters(reader);
        }

        void HandleMotionParamsMsg(NetworkReader reader)
        {
            if (hasAuthority && clientAuthority)
                return;

            ReadParameters(reader);
        }

        void HandleMotionTriggerMsg(int hash)
        {
            m_Motion.motionGraph.GetTriggerProperty(hash).Trigger();
        }

        void HandleMotionResetTriggerMsg(int hash)
        {
            m_Motion.motionGraph.GetTriggerProperty(hash).ResetValue();
        }

        ulong NextDirtyBits()
        {
            ulong dirtyBits = 0;
            for (int i = 0; i < namesParameters.Length; i++)
            {
                bool changed = false;
                if (i < lastFloatParameters.Length)
                {
                    float newFloatValue = m_Motion.motionGraph.GetFloat(Animator.StringToHash(namesParameters[i]));
                    changed = newFloatValue != lastFloatParameters[i];
                    lastFloatParameters[i] = newFloatValue;
                }
                else if (i >= lastFloatParameters.Length 
                && i < (lastFloatParameters.Length + lastIntParameters.Length))
                {
                    int newIntValue = m_Motion.motionGraph.GetInt(Animator.StringToHash(namesParameters[i]));
                    changed = newIntValue != lastIntParameters[i - lastFloatParameters.Length];
                    lastIntParameters[i - lastFloatParameters.Length] = newIntValue;
                }
                else if (i >= (lastFloatParameters.Length + lastIntParameters.Length) 
                && i < (lastFloatParameters.Length + lastIntParameters.Length + lastSwitchParameters.Length))
                {
                    bool newBoolValue = m_Motion.motionGraph.GetSwitch(Animator.StringToHash(namesParameters[i]));
                    changed = newBoolValue != lastSwitchParameters[i - (lastFloatParameters.Length + lastIntParameters.Length)];
                    lastSwitchParameters[i - (lastFloatParameters.Length + lastIntParameters.Length)] = newBoolValue;
                }
                else if (i >= (lastFloatParameters.Length + lastIntParameters.Length + lastSwitchParameters.Length) 
                && i < (lastFloatParameters.Length + lastIntParameters.Length + lastSwitchParameters.Length + lastVectorParameters.Length))
                {
                    Vector3 newVectorValue = m_Motion.motionGraph.GetVector(Animator.StringToHash(namesParameters[i]));
                    changed = newVectorValue != lastVectorParameters[i - (lastFloatParameters.Length + lastIntParameters.Length + lastSwitchParameters.Length)];
                    lastVectorParameters[i - (lastFloatParameters.Length + lastIntParameters.Length + lastSwitchParameters.Length)] = newVectorValue;
                }
                else if (i >= (lastFloatParameters.Length + lastIntParameters.Length + lastSwitchParameters.Length + lastVectorParameters.Length) 
                && i < (lastFloatParameters.Length + lastIntParameters.Length + lastSwitchParameters.Length + lastVectorParameters.Length + lastTransformParameters.Length))
                {
                    Transform newTransformValue = m_Motion.motionGraph.GetTransform(Animator.StringToHash(namesParameters[i]));
                    changed = newTransformValue != lastTransformParameters[i - (lastFloatParameters.Length + lastIntParameters.Length + lastSwitchParameters.Length + lastVectorParameters.Length)];
                    lastTransformParameters[i - (lastFloatParameters.Length + lastIntParameters.Length + lastSwitchParameters.Length + lastVectorParameters.Length)] = newTransformValue;
                }
                if (changed)
                {
                    dirtyBits |= 1ul << i;
                }
            }
            return dirtyBits;
        }

        bool WriteParameters(NetworkWriter writer, bool forceAll = false)
        {
            ulong dirtyBits = forceAll ? (~0ul) : NextDirtyBits();
            writer.WritePackedUInt64(dirtyBits);
            for (int i = 0; i < namesParameters.Length; i++)
            {
                if ((dirtyBits & (1ul << i)) == 0)
                    continue;

                if (i < lastFloatParameters.Length)
                {
                    float newFloatValue = m_Motion.motionGraph.GetFloat(Animator.StringToHash(namesParameters[i]));
                    writer.WriteSingle(newFloatValue);
                }
                else if (i >= lastFloatParameters.Length 
                && i < (lastFloatParameters.Length + lastIntParameters.Length))
                {
                    int newIntValue = m_Motion.motionGraph.GetInt(Animator.StringToHash(namesParameters[i]));
                    writer.WritePackedInt32(newIntValue);
                }
                else if (i >= (lastFloatParameters.Length + lastIntParameters.Length) 
                && i < (lastFloatParameters.Length + lastIntParameters.Length + lastSwitchParameters.Length))
                {
                    bool newBoolValue = m_Motion.motionGraph.GetSwitch(Animator.StringToHash(namesParameters[i]));
                    writer.WriteBoolean(newBoolValue);
                }
                else if (i >= (lastFloatParameters.Length + lastIntParameters.Length + lastSwitchParameters.Length) 
                && i < (lastFloatParameters.Length + lastIntParameters.Length + lastSwitchParameters.Length + lastVectorParameters.Length))
                {
                    Vector3 newVectorValue = m_Motion.motionGraph.GetVector(Animator.StringToHash(namesParameters[i]));
                    writer.WriteVector3(newVectorValue);
                }
                else if (i >= (lastFloatParameters.Length + lastIntParameters.Length + lastSwitchParameters.Length + lastVectorParameters.Length) 
                && i < (lastFloatParameters.Length + lastIntParameters.Length + lastSwitchParameters.Length + lastVectorParameters.Length + lastTransformParameters.Length))
                {
                    Transform newTransformValue = m_Motion.motionGraph.GetTransform(Animator.StringToHash(namesParameters[i]));
                    writer.WriteTransform(newTransformValue);
                }
            }
            return dirtyBits != 0;
        }

        void ReadParameters(NetworkReader reader)
        {
            ulong dirtyBits = reader.ReadPackedUInt64();
            for (int i = 0; i < namesParameters.Length; i++)
            {
                if ((dirtyBits & (1ul << i)) == 0)
                    continue;

                if (i < lastFloatParameters.Length)
                {
                    float newFloatValue = reader.ReadSingle();
                    m_Motion.motionGraph.SetFloat(Animator.StringToHash(namesParameters[i]), newFloatValue);
                }
                else if (i >= lastFloatParameters.Length 
                && i < (lastFloatParameters.Length + lastIntParameters.Length))
                {
                    int newIntValue = reader.ReadPackedInt32();
                    m_Motion.motionGraph.SetInt(Animator.StringToHash(namesParameters[i]), newIntValue);
                }
                else if (i >= (lastFloatParameters.Length + lastIntParameters.Length) 
                && i < (lastFloatParameters.Length + lastIntParameters.Length + lastSwitchParameters.Length))
                {
                    bool newBoolValue = reader.ReadBoolean();
                    m_Motion.motionGraph.SetSwitch(Animator.StringToHash(namesParameters[i]), newBoolValue);
                }
                else if (i >= (lastFloatParameters.Length + lastIntParameters.Length + lastSwitchParameters.Length) 
                && i < (lastFloatParameters.Length + lastIntParameters.Length + lastSwitchParameters.Length + lastVectorParameters.Length))
                {
                    Vector3 newVectorValue = reader.ReadVector3();
                    m_Motion.motionGraph.SetVector(Animator.StringToHash(namesParameters[i]), newVectorValue);
                }
                else if (i >= (lastFloatParameters.Length + lastIntParameters.Length + lastSwitchParameters.Length + lastVectorParameters.Length) 
                && i < (lastFloatParameters.Length + lastIntParameters.Length + lastSwitchParameters.Length + lastVectorParameters.Length + lastTransformParameters.Length))
                {
                    Transform newTransformValue = reader.ReadTransform();
                    m_Motion.motionGraph.SetTransform(Animator.StringToHash(namesParameters[i]), newTransformValue);
                }
            }
        }

        /// <summary>
        /// Custom Serialization
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="initialState"></param>
        /// <returns></returns>
        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.WriteInt32(m_Motion.currentState.serializationKey);
                WriteParameters(writer, initialState);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Custom Deserialization
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="initialState"></param>
        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                int stateHash = reader.ReadInt32();
                //Force State

                ReadParameters(reader);
            }
        }

        /// <summary>
        /// Causes an animation trigger to be invoked for a networked object.
        /// <para>If local authority is set, and this is called from the client, then the trigger will be invoked on the server and all clients. If not, then this is called on the server, and the trigger will be called on all clients.</para>
        /// </summary>
        /// <param name="triggerName">Name of trigger.</param>
        public void SetTrigger(string triggerName)
        {
            SetTrigger(Animator.StringToHash(triggerName));
        }

        /// <summary>
        /// Causes an animation trigger to be invoked for a networked object.
        /// </summary>
        /// <param name="hash">Hash id of trigger (from the Animator).</param>
        public void SetTrigger(int hash)
        {
            if (clientAuthority)
            {
                if (!isClient)
                {
                    logger.LogWarning("Tried to set animation in the server for a client-controlled animator");
                    return;
                }

                if (!hasAuthority)
                {
                    logger.LogWarning("Only the client with authority can set animations");
                    return;
                }

                if (ClientScene.readyConnection != null)
                    CmdOnMotionTriggerServerMessage(hash);
            }
            else
            {
                if (!isServer)
                {
                    logger.LogWarning("Tried to set animation in the client for a server-controlled animator");
                    return;
                }

                RpcOnMotionTriggerClientMessage(hash);
            }
        }

        /// <summary>
        /// Causes an animation trigger to be reset for a networked object.
        /// <para>If local authority is set, and this is called from the client, then the trigger will be reset on the server and all clients. If not, then this is called on the server, and the trigger will be reset on all clients.</para>
        /// </summary>
        /// <param name="triggerName">Name of trigger.</param>
        public void ResetTrigger(string triggerName)
        {
            ResetTrigger(Animator.StringToHash(triggerName));
        }

        /// <summary>
        /// Causes an animation trigger to be reset for a networked object.
        /// </summary>
        /// <param name="hash">Hash id of trigger (from the Animator).</param>
        public void ResetTrigger(int hash)
        {
            if (clientAuthority)
            {
                if (!isClient)
                {
                    logger.LogWarning("Tried to reset animation in the server for a client-controlled animator");
                    return;
                }

                if (!hasAuthority)
                {
                    logger.LogWarning("Only the client with authority can reset animations");
                    return;
                }

                if (ClientScene.readyConnection != null)
                    CmdOnMotionResetTriggerServerMessage(hash);
            }
            else
            {
                if (!isServer)
                {
                    logger.LogWarning("Tried to reset animation in the client for a server-controlled animator");
                    return;
                }

                RpcOnMotionResetTriggerClientMessage(hash);
            }
        }

        #region server message handlers

        [Command]
        void CmdOnMotionServerMessage(int stateHash, byte[] parameters)
        {
            // Ignore messages from client if not in client authority mode
            if (!clientAuthority)
                return;

            if (logger.LogEnabled()) logger.Log("OnAnimationMessage for netId=" + netId);

            // handle and broadcast
            using (PooledNetworkReader networkReader = NetworkReaderPool.GetReader(parameters))
            {
                HandleMotionMsg(stateHash, networkReader);
                RpcOnMotionClientMessage(stateHash, parameters);
            }
        }

        [Command]
        void CmdOnMotionParametersServerMessage(byte[] parameters)
        {
            // Ignore messages from client if not in client authority mode
            if (!clientAuthority)
                return;

            // handle and broadcast
            using (PooledNetworkReader networkReader = NetworkReaderPool.GetReader(parameters))
            {
                HandleMotionParamsMsg(networkReader);
                RpcOnMotionParametersClientMessage(parameters);
            }
        }

        [Command]
        void CmdOnMotionTriggerServerMessage(int hash)
        {
            // Ignore messages from client if not in client authority mode
            if (!clientAuthority)
                return;

            // handle and broadcast
            HandleMotionTriggerMsg(hash);
            RpcOnMotionTriggerClientMessage(hash);
        }

        [Command]
        void CmdOnMotionResetTriggerServerMessage(int hash)
        {
            // Ignore messages from client if not in client authority mode
            if (!clientAuthority)
                return;

            // handle and broadcast
            HandleMotionResetTriggerMsg(hash);
            RpcOnMotionResetTriggerClientMessage(hash);
        }

        #endregion

        #region client message handlers

        [ClientRpc]
        void RpcOnMotionClientMessage(int stateHash, byte[] parameters)
        {
            if (clientAuthority && !syncToOwner && base.hasAuthority)
                return;

            using (PooledNetworkReader networkReader = NetworkReaderPool.GetReader(parameters))
                HandleMotionMsg(stateHash, networkReader);
        }

        [ClientRpc]
        void RpcOnMotionParametersClientMessage(byte[] parameters)
        {
            if (clientAuthority && !syncToOwner && base.hasAuthority)
                return;

            using (PooledNetworkReader networkReader = NetworkReaderPool.GetReader(parameters))
                HandleMotionParamsMsg(networkReader);
        }

        [ClientRpc]
        void RpcOnMotionTriggerClientMessage(int hash)
        {
            if (clientAuthority && !syncToOwner && base.hasAuthority)
                return;

            if (isServer) return;

            HandleMotionTriggerMsg(hash);
        }

        [ClientRpc]
        void RpcOnMotionResetTriggerClientMessage(int hash)
        {
            if (clientAuthority && !syncToOwner && base.hasAuthority)
                return;
            
            if (isServer) return;

            HandleMotionResetTriggerMsg(hash);
        }

        #endregion
    }
}
