using Unity.Netcode;
using UnityEngine;

public class OutfitSelector : NetworkBehaviour
{
    [SerializeField] private Transform _container;
    private NetworkVariable<OutfitPair> _outfitPair = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private int _totalOutfit = -1;
    private int _currentOutfitId = 0;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _outfitPair.OnValueChanged += OnOutfitChanged;
        _totalOutfit = _container.childCount;
    }

    private void OnOutfitChanged(OutfitPair previousValue, OutfitPair newValue)
    {
        // if (previousValue.OutfitId == newValue.OutfitId)
        // {
        //     Transform oldVariant = _container.GetChild(previousValue.OutfitId).GetChild(previousValue.VariantId);
        //     oldVariant.gameObject.SetActive(false);
        // }

        Transform outfit = _container.GetChild(newValue.OutfitId);
        outfit.DisableChildren();
        Transform newVariant = outfit.GetChild(newValue.VariantId);
        newVariant.gameObject.SetActive(true);
    }


    // UI can hook up to this method
    public void OwnerChangeOutfit(int outfitId, int variantId)
    {
        if (!IsOwner)
            return;
        _currentOutfitId = outfitId;
        _outfitPair.Value = new(outfitId, variantId);
    }
    public void OwnerChangeOutfit(int outfitId, bool next)
    {
        if (!IsOwner)
            return;
        _currentOutfitId = outfitId;
        int variantCount = _container.GetChild(_currentOutfitId).childCount;
        if (next)
        {
            int variantId = (_outfitPair.Value.VariantId + 1) % variantCount;
            _outfitPair.Value = new(_currentOutfitId, variantId);
        }
        else
        {
            int variantId = (_outfitPair.Value.VariantId - 1) % variantCount;
            if (variantId < 0)
            {
                variantId = variantCount - 1;
            }
            _outfitPair.Value = new(_currentOutfitId, variantId);
        }
    }

#if UNITY_EDITOR
/*
    void Update()
    {
        if (!IsOwner)
            return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            _currentOutfitId = (_currentOutfitId + 1) % _totalOutfit;
            _outfitPair.Value = new(_currentOutfitId, 0);

        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            _currentOutfitId = (_currentOutfitId - 1) % _totalOutfit;
            if (_currentOutfitId < 0)
            {
                _currentOutfitId = _totalOutfit - 1;
            }
            _outfitPair.Value = new(_currentOutfitId, 0);
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            int variantCount = _container.GetChild(_currentOutfitId).childCount;
            int variantId = (_outfitPair.Value.VariantId + 1) % variantCount;
            _outfitPair.Value = new(_currentOutfitId, variantId);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            int variantCount = _container.GetChild(_currentOutfitId).childCount;
            int variantId = (_outfitPair.Value.VariantId - 1) % variantCount;
            if (variantId < 0)
            {
                variantId = variantCount - 1;
            }
            _outfitPair.Value = new(_currentOutfitId, variantId);
        }
    }
*/
# endif


}

public struct OutfitPair : INetworkSerializable
{
    public int OutfitId;
    public int VariantId;

    public OutfitPair(int outfitId, int variantId)
    {
        OutfitId = outfitId;
        VariantId = variantId;
    }
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue<int>(ref OutfitId);
        serializer.SerializeValue<int>(ref VariantId);
    }
}