using UnityEngine;
using NeoSaveGames.Serialization;
using NeoSaveGames;

namespace NeoFPS.ModularFirearms
{
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-ballisticprojectile.html")]
	[RequireComponent (typeof (PooledObject))]
	public class NetBallisticProjectile : MonoBehaviour, IProjectile, INeoSerializableComponent
	{
		[SerializeField, Tooltip("The minimum distance before the projectile will appear.")]
		private float m_MinDistance = 0f;

		[SerializeField, Tooltip("Should the projectile rotate so it is always facing down the curve.")]
		private bool m_FollowCurve = false;

		[SerializeField, Tooltip("Forget the character's \"ignore root\", meaning it can detonate on the character collider.")]
        private bool m_ForgetIgnoreRoot = false;

        [SerializeField, Tooltip("The time after the bullet hits an object before it is returned to the pool (allows trail renderers to complete).")]
        private float m_RecycleDelay = 0.5f;

        private const float k_MaxDistance = 10000f;

        private Vector3 m_Velocity = Vector3.zero;
        private Vector3 m_CatchupDistance = Vector3.zero;
		private IAmmoEffect m_AmmoEffect = null;
        private IDamageSource m_DamageSource = null;
        private Transform m_IgnoreRoot = null;
        private LayerMask m_Layers;
        private PooledObject m_PooledObject = null;
        private MeshRenderer m_MeshRenderer = null;
        private bool m_Release = false;
        private bool m_PassedMinimum = false;
        private float m_Distance = 0f;
		private float m_LerpTime = -1f;
        private float m_Timeout = 0f;
        private Vector3 m_LerpFromPosition = Vector3.zero;
        private Vector3 m_LerpToPosition = Vector3.zero;

        private RaycastHit m_Hit = new RaycastHit();

		public Transform localTransform
		{
			get;
			private set;
		}

        public float gravity
        {
            get;
            private set;
        }

        public float distanceTravelled
        {
            get { return m_Distance; }
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (m_MinDistance < 0f)
                m_MinDistance = 0f;
        }
#endif

        protected virtual void Awake ()
		{
			localTransform = transform;
			m_PooledObject = GetComponent<PooledObject> ();
			m_MeshRenderer = GetComponentInChildren<MeshRenderer> ();
            if (m_MeshRenderer != null)
                m_MeshRenderer.enabled = false;
        }

        public virtual void SetCatchup(float duration, Vector3 velocity, float gravity)
        {
#if FIRSTGEARGAMES_COLLIDERROLLBACKS
            m_CatchupDistance = ((duration/Time.fixedDeltaTime) * velocity);
#else
            m_CatchupDistance = (duration * velocity);
#endif
            //m_CatchupDistance.y -= (gravity * duration);
        }

		public virtual void Fire (Vector3 position, Vector3 velocity, float gravity, IAmmoEffect effect, Transform ignoreRoot, LayerMask layers, IDamageSource damageSource = null)
		{
			m_Velocity = velocity;
			this.gravity = gravity;
			m_AmmoEffect = effect;
			m_DamageSource = damageSource;
            m_IgnoreRoot = ignoreRoot;
            m_Layers = layers;

            localTransform.position = position;
			if (m_FollowCurve)
				localTransform.LookAt (position + velocity);
			
			// Reset distance
			m_Distance = 0;
            m_PassedMinimum = false;

            // Reset pooling
            m_Release = false;
            m_Timeout = m_RecycleDelay;

            // Hide the mesh for the first frame
            if (m_MeshRenderer != null)
                m_MeshRenderer.enabled = false;

			// Store the starting position
			m_LerpToPosition = localTransform.position;

			// Update immediately
			FixedUpdate ();
		}

		void FixedUpdate ()
		{
			if (m_Release)
			{
                if (m_RecycleDelay <= 0f)
                    ReleaseProjectile();
                else
                {
                    if (m_MeshRenderer != null && m_MeshRenderer.enabled)
                        m_MeshRenderer.enabled = false;
                    m_Timeout -= Time.deltaTime;
                    if (m_Timeout < 0f)
                        ReleaseProjectile();
                }
			}
			else
			{
                
                float time = Time.deltaTime;

				// Set position to target
				localTransform.position = m_LerpToPosition;

				// Reset interpolation for Update() frames before next fixed
				m_LerpTime = Time.fixedTime;
				m_LerpFromPosition = m_LerpToPosition;

                Vector3 moveVelocity = (m_Velocity * time);
                Vector3 catchupValue = Vector3.zero;

                if(m_CatchupDistance.magnitude > 0f)
                {
                    Vector3 steps = (m_CatchupDistance * time);
                    
                    m_CatchupDistance -= steps;

                    if(m_CatchupDistance.magnitude < (m_Velocity * 0.1f).magnitude)
                    {
                       catchupValue += m_CatchupDistance;
                       m_CatchupDistance = Vector3.zero;
                    }
                }

                
				Vector3 desiredPosition = m_LerpFromPosition + (moveVelocity + catchupValue);
				float distance = Vector3.Distance (m_LerpFromPosition, desiredPosition);
				localTransform.LookAt (desiredPosition);

                // Enable renderer if travelled far enough (check based on from position due to lerp)
                if (!m_PassedMinimum && m_Distance > m_MinDistance)
                {
                    m_PassedMinimum = true;

                    if (m_ForgetIgnoreRoot)
                        m_IgnoreRoot = null;

                    if (m_MeshRenderer != null && m_MeshRenderer.enabled == false)
                        m_MeshRenderer.enabled = true;
                }

                Ray ray = new Ray (localTransform.position, localTransform.forward);
				if (PhysicsExtensions.RaycastNonAllocSingle (ray, out m_Hit, distance, m_Layers, m_IgnoreRoot, QueryTriggerInteraction.Ignore))
				{
					// Set lerp target
					m_LerpToPosition = m_Hit.point;

					// Release back to pool 
					m_Release = true;

                    // Update distance travelled
                    m_Distance += m_Hit.distance;

                    m_AmmoEffect.Hit (m_Hit, localTransform.forward, m_Distance, m_Velocity.magnitude, m_DamageSource);

                    OnHit();

                }
				else
				{
					// Set lerp target
					m_LerpToPosition = desiredPosition;

					// Update distance travelled
					m_Distance += distance;

					// Should the bullet just give up and retire?
					if (m_Distance > k_MaxDistance)
						ReleaseProjectile ();
				}

                // Apply forces to the projectile
                m_Velocity = ApplyForces(m_Velocity);
            }
		}

        protected virtual void OnHit() {}

        protected virtual Vector3 ApplyForces(Vector3 v)
        {
            // Add gravity
            v.y -= gravity * Time.deltaTime;
            return v;
        }

		void ReleaseProjectile ()
		{
			m_PooledObject.ReturnToPool ();
		}

		void Update ()
		{
			// Get lerp value
			float elapsed = Time.time - m_LerpTime;
			float lerp = elapsed / Time.fixedDeltaTime;

			// Lerp the position towards the target
			localTransform.position = Vector3.Lerp (m_LerpFromPosition, m_LerpToPosition, lerp);
		}

        private static readonly NeoSerializationKey k_VelocityKey = new NeoSerializationKey("velocity");
        private static readonly NeoSerializationKey k_LayersKey = new NeoSerializationKey("layers");
        private static readonly NeoSerializationKey k_ReleaseKey = new NeoSerializationKey("release");
        private static readonly NeoSerializationKey k_DistanceKey = new NeoSerializationKey("distance");
        private static readonly NeoSerializationKey k_PositionKey = new NeoSerializationKey("position");
        private static readonly NeoSerializationKey k_DamageSourceKey = new NeoSerializationKey("damageSource");
        private static readonly NeoSerializationKey k_AmmoEffectKey = new NeoSerializationKey("ammoEffect");

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            writer.WriteValue(k_VelocityKey, m_Velocity);
            writer.WriteValue(k_LayersKey, m_Layers);
            writer.WriteValue(k_ReleaseKey, m_Release);
            writer.WriteValue(k_DistanceKey, m_Distance);
            writer.WriteValue(k_PositionKey, m_LerpToPosition);

            writer.WriteComponentReference(k_AmmoEffectKey, m_AmmoEffect, nsgo);
            writer.WriteComponentReference(k_DamageSourceKey, m_DamageSource, nsgo);
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            reader.TryReadValue(k_VelocityKey, out m_Velocity, m_Velocity);
            reader.TryReadValue(k_ReleaseKey, out m_Release, m_Release);
            reader.TryReadValue(k_DistanceKey, out m_Distance, m_Distance);

            int layerMask = m_Layers;
            if (reader.TryReadValue(k_LayersKey, out layerMask, layerMask))
                m_Layers = layerMask;

            Vector3 position;
            if (reader.TryReadValue(k_PositionKey, out position, Vector3.zero))
            {
                m_LerpFromPosition = position;
                m_LerpToPosition = position;
                localTransform.position = position;
            }

            IAmmoEffect serializedAmmoEffect;
            if (reader.TryReadComponentReference(k_AmmoEffectKey, out serializedAmmoEffect, nsgo))
                m_AmmoEffect = serializedAmmoEffect;
            IDamageSource serializedDamageSource;
            if (reader.TryReadComponentReference(k_DamageSourceKey, out serializedDamageSource, nsgo))
                m_DamageSource = serializedDamageSource;
        }
    }
}