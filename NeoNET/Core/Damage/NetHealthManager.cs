using Mirror;
using System;
using UnityEngine;
using NeoSaveGames.Serialization;
using NeoSaveGames;
using UnityEngine.Events;

namespace NeoFPS.Mirror
{
    [HelpURL("https://docs.neofps.com/manual/healthref-mb-basichealthmanager.html")]
    /*
    Notes: Below is based on HealthManager

    Things to work

    Check over the Damage Passing,
    Possibly stop health sycn to all, and only sync between server and owner

    Pass Send dead and alive signals.

    */
    public class NetHealthManager : NetworkBehaviour, IHealthManager, INeoSerializableComponent
    {
        [Tooltip("The starting health of the character.")]
        [SerializeField] private float m_Health = 100f;
        [Tooltip("The maximum health of the character.")]
        [SerializeField] private float m_HealthMax = 100f;
        [Tooltip("An event called whenever the health changes")]
        [SerializeField] private FloatEvent m_OnHealthChanged = null;
        [Tooltip("An event called whenever the alive state of the health manager changes")]
        [SerializeField] private BoolEvent m_OnIsAliveChanged = null;

        private static readonly NeoSerializationKey k_HealthKey = new NeoSerializationKey("health");
        private static readonly NeoSerializationKey k_HealthMaxKey = new NeoSerializationKey("healthMax");
        private static readonly NeoSerializationKey k_IsAliveKey = new NeoSerializationKey("isAlive");

        public event HealthDelegates.OnIsAliveChanged onIsAliveChanged;
        public event HealthDelegates.OnHealthChanged onHealthChanged;
        public event HealthDelegates.OnHealthMaxChanged onHealthMaxChanged;

        [Serializable]
        public class FloatEvent : UnityEvent<float>
        {
        }

        [Serializable]
        public class BoolEvent : UnityEvent<bool>
        {
        }

        private bool m_IsAlive = true;
        public bool isAlive
        {
            get { return m_IsAlive; }
            protected set
            {
                if (m_IsAlive != value)
                {
                    m_IsAlive = value;
                    if (onIsAliveChanged != null)
                        onIsAliveChanged(m_IsAlive);
                    m_OnIsAliveChanged.Invoke(m_IsAlive);
                }
            }
        }

        public float health
        {
            get { return m_Health; }
            set { SetHealth(value, false, null); }
        }

        public float healthMax
        {
            get { return m_HealthMax; }
            set
            {
                if (m_HealthMax != value)
                {
                    float old = m_HealthMax;
                    // Set value
                    m_HealthMax = value;
                    // Check lower limit
                    if (m_HealthMax < 0f)
                        m_HealthMax = 0f;
                    // Fire event
                    if (onHealthMaxChanged != null)
                        onHealthMaxChanged(old, m_HealthMax);
                    // Check health is still valid
                    if (health > m_HealthMax)
                        health = m_HealthMax;
                }
            }
        }
        // Send server Damage as a diffrent command to aviod loops?
    
        protected void SetHealth(float h, bool critical, IDamageSource source)
        {
            float old = m_Health;
            Debug.Log("SetHealth: "+h);
            if(source != null &&(source is ModularFirearms.IModularFirearm || source is IMeleeWeapon || source is IThrownWeapon)){
                if(!base.isServer)
                    return;

                ApplyHealth(h, critical, source);
                RpcHealthUpdate(m_Health, critical);//, source);
            }else{
                if(!base.hasAuthority)
                    return;

                ApplyHealth(h, critical, source);
                if(!base.isServer){
                    CmdHealthUpdate(m_Health, critical);//, source);
                }else{
                    RpcHealthUpdate(m_Health, critical);//, source);
                }
            }
            
            if (m_Health != old)
            {
                OnHealthChanged(old, m_Health, critical, source);
                // Check if dead
                if (Mathf.Approximately(m_Health, 0f) && isAlive)
                    isAlive = false;
            }
        }

        private void ApplyHealth(float h, bool critical, IDamageSource source){
            m_Health = Mathf.Clamp(h, 0f, m_HealthMax);
            // Put Damage area
        }

        protected virtual void OnHealthChanged(float from, float to, bool critical, IDamageSource source)
        {
            // Fire event
            if (onHealthChanged != null)
                onHealthChanged(from, to, critical, source);
            m_OnHealthChanged.Invoke(to);
        }

        public virtual void AddDamage(float damage)
        {
            SetHealth(health - damage, false, null);
        }
        public virtual void AddDamage(float damage, bool critical)
        {
            SetHealth(health - damage, critical, null);
        }
        public virtual void AddDamage(float damage, IDamageSource source)
        {
            SetHealth(health - damage, false, source);
        }
        public virtual void AddDamage(float damage, bool critical, IDamageSource source)
        {
            SetHealth(health - damage, critical, source);
        }
        public virtual void AddHealth(float h)
        {
            SetHealth(health + h, false, null);

            // Check if brought back to life
            if (!isAlive && health > 0f)
                isAlive = true;
        }
        public virtual void AddHealth(float h, IDamageSource source)
        {
            // Change health
            SetHealth(health + h, false, source);

            // Check if brought back to life
            if (!isAlive && health > 0f)
                isAlive = true;
        }

        [Command]
        private void CmdHealthUpdate(float h, bool critical)//, IDamageSource source)
        {
            float old = m_Health;
            Debug.Log("Player Says Damage: "+h);

            ApplyHealth(h, critical, null);
            RpcHealthRelay(m_Health, critical);//, source);

            if (m_Health != old)
            {
                OnHealthChanged(old, m_Health, critical, null);
                // Check if dead
                if (Mathf.Approximately(m_Health, 0f) && isAlive)
                    isAlive = false;
            }
        }

        [ClientRpc]
        private void RpcHealthUpdate(float h, bool critical)//, IDamageSource source)
        {
            if(base.isServer)
                return;

            Debug.Log("Server Says Damage: "+h);
            float old = m_Health;

            ApplyHealth(h, critical, null);

            if (m_Health != old)
            {
                OnHealthChanged(old, m_Health, critical, null);
                // Check if dead
                if (Mathf.Approximately(m_Health, 0f) && isAlive)
                    isAlive = false;
            }
        }

        [ClientRpc(excludeOwner = true)]
        private void RpcHealthRelay(float h, bool critical)//, IDamageSource source)
        {
            if(base.isServer || base.hasAuthority)
                return;

            float old = m_Health;

            ApplyHealth(h, critical, null);

            if (m_Health != old)
            {
                OnHealthChanged(old, m_Health, critical, null);
                // Check if dead
                if (Mathf.Approximately(m_Health, 0f) && isAlive)
                    isAlive = false;
            }
        }

        /// INeoSerializableComponent Implenation
        public virtual void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            writer.WriteValue(k_HealthMaxKey, healthMax);
            writer.WriteValue(k_HealthKey, health);
            writer.WriteValue(k_IsAliveKey, isAlive);
        }

        public virtual void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            float floatValue = 0f;
            if (reader.TryReadValue(k_HealthMaxKey, out floatValue, healthMax))
                healthMax = floatValue;
            if (reader.TryReadValue(k_HealthKey, out floatValue, health))
                health = floatValue;

            bool boolValue = true;
            if (reader.TryReadValue(k_IsAliveKey, out boolValue, boolValue))
                isAlive = boolValue;
        }
    }
}
