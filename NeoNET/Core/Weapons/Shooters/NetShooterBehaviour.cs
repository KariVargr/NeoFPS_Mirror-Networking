using UnityEngine;
using UnityEngine.Events;
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

		public virtual void NetShoot (IAmmoEffect effect, float rollbackTime, Vector3 position, Vector3 forward)
		{
			SendOnShootEvent();
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