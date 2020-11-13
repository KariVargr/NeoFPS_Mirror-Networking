using System.Collections.Generic;
using UnityEngine;
using NeoFPS.Mirror.NetworkPlayer;
using System.Collections;

namespace NeoFPS.Mirror
{
    public class FpsNetPlayerCharacterEventWatcher : MonoBehaviour, IPlayerCharacterWatcher
    {
        private List<IPlayerCharacterSubscriber> m_Subsribers = new List<IPlayerCharacterSubscriber>(4);
        private FpsNetCharacter m_CurrentCharacter = null;

        IEnumerator Start()
        {
            yield return null;
            FpsNetCharacter.onLocalPlayerCharacterChange += OnLocalPlayerCharacterChange;
            OnLocalPlayerCharacterChange(FpsNetCharacter.localPlayerCharacter);
        }

        void OnDestroy()
        {
            FpsNetCharacter.onLocalPlayerCharacterChange -= OnLocalPlayerCharacterChange;
        }

        public void AttachSubscriber(IPlayerCharacterSubscriber subscriber)
        {
            if (subscriber == null)
                return;

            if (!m_Subsribers.Contains(subscriber))
            {
                m_Subsribers.Add(subscriber);
                subscriber.OnPlayerCharacterChanged(m_CurrentCharacter);
            }
            else
                Debug.LogError("Attempting to attach a player inventory subscriber that is already attached.");
        }

        public void ReleaseSubscriber(IPlayerCharacterSubscriber subscriber)
        {
            if (subscriber == null)
                return;

            if (m_Subsribers.Contains(subscriber))
                m_Subsribers.Remove(subscriber);
            else
                Debug.LogError("Attempting to remove a player inventory subscriber that was not attached.");
        }

        void OnLocalPlayerCharacterChange(FpsNetCharacter character)
        {
            m_CurrentCharacter = character;
            for (int i = 0; i < m_Subsribers.Count; ++i)
                m_Subsribers[i].OnPlayerCharacterChanged(m_CurrentCharacter);
        }
    }
}
