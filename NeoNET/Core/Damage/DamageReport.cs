using NeoFPS;
using UnityEngine;

namespace NeoFPS
{
    public class DamageReport : IDamageSource
	{
		public DamageReport(DamageFilter filter, IController control, Transform weapon, string desc)
        {
            outDamageFilter = filter;
            controller = control;
            damageSourceTransform = weapon;
            description = desc;
            // possibly look in weapon for contoller is controller = null
        }

        #region IDamageSource implementation
		private DamageFilter m_OutDamageFilter = DamageFilter.AllDamageAllTeams;
		public DamageFilter outDamageFilter
		{
			get{ return m_OutDamageFilter; }
            set{ m_OutDamageFilter = value; }
		}

		public IController controller
		{
			get;
            private set;
		}

		public Transform damageSourceTransform
		{
			get;
            private set;
		}

		public string description
		{
			get;
            private set;
		}

		#endregion
	}
}