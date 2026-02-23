using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// This class is used for Events that have no arguments (Example: Start game event)
/// </summary>

[CreateAssetMenu(menuName = "Events/Void Event Channel")]
public class VoidEventChannelSO : DescriptionBaseSO
{
    public UnityAction OnEventRaised = delegate { };

    public void RaiseEvent() => OnEventRaised.Invoke();
}



