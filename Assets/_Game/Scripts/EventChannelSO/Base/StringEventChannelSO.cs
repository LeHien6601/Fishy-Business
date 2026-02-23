using UnityEngine.Events;
using UnityEngine;

/// <summary>
/// This class is used for Events that have a String argument.
/// </summary>

[CreateAssetMenu(menuName = "Events/String Event Channel")]
public class StringEventChannelSO : DescriptionBaseSO
{
	public event UnityAction<string> OnEventRaised = delegate { };

	public void RaiseEvent(string value) => OnEventRaised.Invoke(value);
}
