using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Voice Activity Event Channel")]
public class VoiceActivityEventChannelSO : DescriptionBaseSO
{
    public event UnityAction<string, bool> OnEventRaised = delegate { };

	public void RaiseEvent(string playerId, bool isSpeaking) => OnEventRaised.Invoke(playerId, isSpeaking);
}