using Unity.Netcode;
using UnityEngine;

public class OutfitSelector : NetworkBehaviour
{
    [SerializeField] private Transform _container;
    private NetworkVariable<OutfitPair> _outfitPair = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private int _currentOutfitId = 0;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _outfitPair.OnValueChanged += OnOutfitChanged;

        OnOutfitChanged(default, _outfitPair.Value);
    }

    private void OnOutfitChanged(OutfitPair previousValue, OutfitPair newValue)
    {
        Transform outfit = _container.GetChild(newValue.OutfitId);
        outfit.DisableChildren();
        Transform newVariant = outfit.GetChild(newValue.VariantId);
        newVariant.gameObject.SetActive(true);
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