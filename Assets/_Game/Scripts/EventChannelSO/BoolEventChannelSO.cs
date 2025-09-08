using UnityEngine.Events;
using UnityEngine;

/// <summary>
/// This class is used for Events that have a bool argument.
/// Example: An event to toggle a UI interface
/// </summary>

[CreateAssetMenu(menuName = "Events/Bool Event Channel")]
public class BoolEventChannelSO : DescriptionBaseSO
{
	public event UnityAction<bool> OnEventRaised = delegate { };

	public void RaiseEvent(bool value) => OnEventRaised.Invoke(value);
}
