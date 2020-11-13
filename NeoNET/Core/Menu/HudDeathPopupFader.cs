using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using NeoFPS.SinglePlayer;
using NeoFPS;

namespace NeoFPS.Mirror.Menu
{
    [HelpURL("https://docs.neofps.com/manual/hudref-mb-huddeathpopup.html")]
    [RequireComponent (typeof (CanvasGroup))]
	public class HudDeathPopupFader : PlayerCharacterHudBase
    {
        private CanvasGroup m_CanvasGroup = null;
		private ICharacter m_Character = null;
        [SerializeField] private float m_DeathFade = 2;
        [SerializeField] private float m_FadeDelay = 1;
        protected override void Awake()
        {
            base.Awake();
            m_CanvasGroup = GetComponent<CanvasGroup>();
		}

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // Unsubscribe from old character
            if (m_Character != null)
                m_Character.onIsAliveChanged -= OnIsAliveChanged;
        }

        public override void OnPlayerCharacterChanged(ICharacter character)
        {
			if (m_Character != null)
				m_Character.onIsAliveChanged -= OnIsAliveChanged;

			m_Character = character;

			if (m_Character as Component != null)
			{
				m_Character.onIsAliveChanged += OnIsAliveChanged;
				OnIsAliveChanged (m_Character, m_Character.isAlive);
			}
			else
				gameObject.SetActive (false);
		}

		void OnIsAliveChanged (ICharacter character, bool alive)
		{    
			gameObject.SetActive (!alive);

            if(!alive)
                StartCoroutine(DeathPopupFader());
		}

        private IEnumerator DeathPopupFader()
        {

            while (m_CanvasGroup.alpha < 1f){
                yield return null;
                m_CanvasGroup.alpha += Time.deltaTime / m_DeathFade;
                if (m_CanvasGroup.alpha > 1f)
                    m_CanvasGroup.alpha = 1f;
            }
            yield return new WaitForSeconds(m_FadeDelay);
            while (m_CanvasGroup.alpha > 0f){
                yield return null;
                m_CanvasGroup.alpha -= Time.deltaTime / m_DeathFade;
                if (m_CanvasGroup.alpha < 0f)
                    m_CanvasGroup.alpha = 0f;
            }
        }
	}
}