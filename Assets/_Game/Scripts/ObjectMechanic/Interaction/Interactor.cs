using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public interface IInteractable
{
    public void Interact(PlayerController actor);
}

/// <summary>
/// just a trigger collider to detect objects in interactable layer (expect these implement IInteractable)
/// then call their Interact method when the player presses the interact button
/// 
/// concrete behavior of Interact is defined in the specific IInteractable implementation
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class Interactor : MonoBehaviour
{
    [SerializeField] private PlayerController _host;
    [SerializeField] private InputReaderSO _inputReader;
    [SerializeField] private List<GameObject> _potentialInteractions = new(); //To store the objects the player could potentially interact with

    private void OnEnable()
    {
        _inputReader.Interact += Interact;
    }

    private void OnDisable()
    {
        _inputReader.Interact -= Interact;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == Constant.INTERACTABLE_LAYER)
        {
            Debug.Log($"Interactor: Entered trigger with {other.gameObject.name}");
            _potentialInteractions.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == Constant.INTERACTABLE_LAYER)
        {
            _potentialInteractions.Remove(other.gameObject);
        }
    }

    public void Interact()
    {
        Debug.Log("Interactor: Interact called");
        if (_potentialInteractions.Count == 0)
            return;

        IInteractable i = _potentialInteractions[0].GetComponent<IInteractable>(); ;
        _potentialInteractions.RemoveAt(0);
        i.Interact(_host);
    }
}