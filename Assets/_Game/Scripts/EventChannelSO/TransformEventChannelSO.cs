using UnityEngine.Events;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/Transform Event Channel")]
public class TransformEventChannelSO : DescriptionBaseSO
{
    public UnityAction<Transform> OnEventRaised = delegate { };

    public void RaiseEvent(Transform value) => OnEventRaised.Invoke(value);
}
