using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// This class is used for Events that have one float argument.
/// Example: An player health changed event, need to seed to an UI element
/// </summary>

[CreateAssetMenu(menuName = "Events/Float Event Channel")]
public class FloatEventChannelSO : DescriptionBaseSO
{
    public UnityAction<float> OnEventRaised = delegate { };

    public void RaiseEvent(float value) => OnEventRaised.Invoke(value);
}
