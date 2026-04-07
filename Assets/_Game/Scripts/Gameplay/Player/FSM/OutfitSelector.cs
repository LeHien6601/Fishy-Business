using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class OutfitSelector : NetworkBehaviour
{
    [SerializeField] private Transform _container;

    private NetworkList<int> _outfitComboNet;

    private int _currentOutfitId = 0;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _outfitComboNet.OnListChanged += OnOutfitComboChanged;

        if (IsOwner)
        {
            LoadOrCreateDefaultOutfit();
        }

        ApplyAllOutfitsFromNetworkList();
    }

    public override void OnNetworkDespawn()
    {
        if (_outfitComboNet != null)
            _outfitComboNet.OnListChanged -= OnOutfitComboChanged;

        base.OnNetworkDespawn();
    }

    private void Awake()
    {
        _outfitComboNet = new NetworkList<int>(
            null,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner   // Owner can change their own outfit
        );
    }
    [System.Serializable]
    private class OutfitSaveData { public List<int> variants; }

    private void LoadOrCreateDefaultOutfit()
    {
        List<int> loaded = new List<int>();

        if (PlayerPrefs.HasKey(Constant.KEY_PLAYER_OUTFIT))
        {
            var data = JsonUtility.FromJson<OutfitSaveData>(
                PlayerPrefs.GetString(Constant.KEY_PLAYER_OUTFIT)
            );
            if (data?.variants != null) loaded = data.variants;
        }

        int count = _container.childCount;
        while (loaded.Count < count) loaded.Add(0);
        while (loaded.Count > count) loaded.RemoveAt(loaded.Count - 1);

        foreach (int variant in loaded)
            _outfitComboNet.Add(variant);
    }

    private void OnOutfitComboChanged(NetworkListEvent<int> changeEvent)
    {
        if (changeEvent.Type == NetworkListEvent<int>.EventType.Value)
        {
            ApplyOutfit(changeEvent.Index, changeEvent.Value);
        }
        else if (changeEvent.Type == NetworkListEvent<int>.EventType.Add ||
                 changeEvent.Type == NetworkListEvent<int>.EventType.Insert)
        {
            ApplyOutfit(changeEvent.Index, changeEvent.Value);
        }
    }

    private void ApplyOutfit(int outfitId, int variantId)
    {
        if (outfitId < 0 || outfitId >= _container.childCount) return;

        Transform outfit = _container.GetChild(outfitId);
        outfit.DisableChildren();

        if (variantId < outfit.childCount)
        {
            outfit.GetChild(variantId).gameObject.SetActive(true);
            Debug.Log($"Applied outfit {outfitId} variant {variantId}");
        }
    }

    private void ApplyAllOutfitsFromNetworkList()
    {
        for (int i = 0; i < _outfitComboNet.Count && i < _container.childCount; i++)
        {
            ApplyOutfit(i, _outfitComboNet[i]);
        }
    }

    public void OwnerChangeOutfit(int outfitId, bool next)
    {
        if (!IsOwner || outfitId < 0 || outfitId >= _outfitComboNet.Count)
            return;

        _currentOutfitId = outfitId;

        int variantCount = _container.GetChild(outfitId).childCount;
        int currentVariant = _outfitComboNet[outfitId];

        int newVariant = next
            ? (currentVariant + 1) % variantCount
            : (currentVariant - 1 + variantCount) % variantCount;

        // This automatically syncs to everyone
        _outfitComboNet[outfitId] = newVariant;

        ApplyOutfit(outfitId, newVariant); // Immediate local feedback
    }

    public void OwnerSaveOutfit()
    {
        if (!IsOwner) return;

        List<int> localCopy = new List<int>(_outfitComboNet.Count);
        foreach (var v in _outfitComboNet)
            localCopy.Add(v);



        var data = new OutfitSaveData { variants = new List<int>(localCopy) };

        PlayerPrefs.SetString(Constant.KEY_PLAYER_OUTFIT, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        Debug.Log("Outfit saved to PlayerPrefs");
    }
}