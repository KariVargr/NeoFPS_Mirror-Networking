using UnityEngine;
using UnityEngine.Events;
using System;
#if FIRSTGEARGAMES_COLLIDERROLLBACKS
using FirstGearGames.Mirrors.Assets.ColliderRollbacks;
#else
using NeoFPS.Mirror.LagCompensation;
#endif
using NeoSaveGames.Serialization;
using NeoSaveGames;

namespace NeoFPS.ModularFirearms
{
	public abstract class NetShooterBehaviour : BaseShooterBehaviour
    {
		public event UnityAction<Vector3, Vector3> onNetShoot;

		protected void SendNetShootEvent (Vector3 position, Vector3 forward)
		{
			if (onNetShoot != null)
				onNetShoot (position, forward);
        }

		public virtual void NetShoot (IAmmoEffect effect, float delay, Vector3 position, Vector3 forward)
		{
			SendOnShootEvent();
		}

		protected void StartRayRollback (float timeDelay)
		{
#if FIRSTGEARGAMES_COLLIDERROLLBACKS
			Debug.Log("RollbackCalled "+timeDelay);
			if(timeDelay > 0f)
				RollbackManager.RollbackSteps(timeDelay, RollbackManager.PhysicsTypes.ThreeDimensional);
#else
			if(timeDelay > 0f)
                LagCompensationManager.StartSimulation(timeDelay);
#endif
		}

		protected void StopRayRollback (float timeDelay)
		{
#if FIRSTGEARGAMES_COLLIDERROLLBACKS
			if(timeDelay > 0f)
				RollbackManager.ReturnForward();
#else
			if(timeDelay > 0f)
                LagCompensationManager.StopSimulation();
#endif
		}

		protected Transform GetRootTransform()
        {
            var t = transform;
            while (t.parent != null)
                t = t.parent;
            return t;
        }
    }
}