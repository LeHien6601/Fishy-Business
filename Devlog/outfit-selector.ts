import imageImport from "@/assets/avatars.png";
import { DevlogEntry } from "@/data/devlog";

export const outfitSelectorDevlog: DevlogEntry = {
  id: "outfitselector-persistent-sync-2026-05-03",
  title: "OutfitSelector: Persistent & Synchronized Player Outfits",
  teaser: "How `OutfitSelector` saves player choices locally and reliably syncs them for late joiners.",
  image: imageImport,
  date: "2026-05-03",
  content: `# OutfitSelector: Persistent & Synchronized Player Outfits

## Introduction

Unity Netcode projects often need to keep player customization consistent across sessions and across the network. \`OutfitSelector.cs\` solves two common UX problems:

- Remembering a player’s preferred outfit between play sessions (so they don’t have to re-select every time).
- Ensuring the player’s outfit is visible to everyone, including players who join the lobby late.

Below I explain the key parts of the implementation and embed the exact C# snippets from the component for clarity.

---

## How saved outfits are handled (persisting preferences)

To avoid forcing players to pick their favorite outfit every time they join, \`OutfitSelector\` persists owner-selected variants using Unity’s \`PlayerPrefs\` as JSON. A small serializable container, \`OutfitSaveData\`, stores a list of variant indices and is written/read with \`JsonUtility\`.

Key snippet (loading or creating a default outfit on owner spawn):

\`\`\`csharp
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
\`\`\`

When the local owner wants to save their current outfit selection, \`OwnerSaveOutfit()\` serializes \`_outfitComboNet\` to PlayerPrefs:

\`\`\`csharp
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
\`\`\`

Why this works well:
- The owner writes a compact list of integers (one per outfit slot). Small and robust.
- On first load (or if saved data is missing/outdated), the code pads/truncates the list to match the number of outfit slots in \`_container\`.

---

## How network sync & late joiners are handled

The component uses a \`NetworkList<int>\` called \`_outfitComboNet\` to hold the chosen variant index for each outfit slot. NetworkLists are part of Unity Netcode’s synchronized collections — they replicate state to newly-connected clients automatically.

Important parts:

- Initialization in \`Awake()\` sets up the NetworkList permissions (Everyone can read; Owner writes).
- When the owner spawns, they call \`LoadOrCreateDefaultOutfit()\` to populate the NetworkList with saved values.
- The script subscribes to \`_outfitComboNet.OnListChanged\` so changes are applied locally (and on all clients when the list updates).

Initialization & spawn logic:

\`\`\`csharp
private void Awake()
{
	_outfitComboNet = new NetworkList<int>(
		null,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Owner
	);
}

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
\`\`\`

How the list changes are applied:

\`\`\`csharp
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

private void ApplyAllOutfitsFromNetworkList()
{
	for (int i = 0; i < _outfitComboNet.Count && i < _container.childCount; i++)
	{
		ApplyOutfit(i, _outfitComboNet[i]);
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
		Debug.Log(\$"Applied outfit {outfitId} variant {variantId}");
	}
}
\`\`\`

Why late joiners see the right outfit:
- NetworkList is a synced collection. When a client connects/spawns, Netcode pushes the current NetworkList contents to that client.
- \`ApplyAllOutfitsFromNetworkList()\` runs on spawn to apply whatever values are present locally (for both host and clients).
- The \`OnListChanged\` subscription ensures that if any owner later changes a variant, all clients (including those who joined earlier or later) receive updates and re-apply the correct visuals.

---

## Owner controls and immediate local feedback

The owner alone can change their outfit (write permission). The approach used gives immediate local feedback while relying on Netcode to broadcast the change:

\`\`\`csharp
public void OwnerChangeOutfit(int outfitId, bool next)
{
	if (!IsOwner || outfitId < 0 || outfitId >= _outfitComboNet.Count)
		return;

	int variantCount = _container.GetChild(outfitId).childCount;
	int currentVariant = _outfitComboNet[outfitId];

	int newVariant = next
		? (currentVariant + 1) % variantCount
		: (currentVariant - 1 + variantCount) % variantCount;

	// This automatically syncs to everyone
	_outfitComboNet[outfitId] = newVariant;

	ApplyOutfit(outfitId, newVariant); // Immediate local feedback
}
\`\`\`

Notes:
- Setting \`_outfitComboNet[outfitId]\` updates the NetworkList; Netcode then replicates the change to other clients.
- Calling \`ApplyOutfit\` immediately gives the local player instant responsiveness rather than waiting for the round-trip network update.

---

## Edge cases and design notes

- Initialization order: \`LoadOrCreateDefaultOutfit()\` adds items to the NetworkList only for the owner on spawn. Because NetworkList synchronizes state to new clients, late joiners receive the owner’s list values.
- If the owner’s saved list length is mismatched with the current scene’s outfit slots (e.g., new outfits added in an update), the code pads/trims to match \`_container.childCount\` to avoid index errors.
- The code uses \`NetworkVariableWritePermission.Owner\`, which is the simplest model for per-player customizations: each player owns and writes their own list; everyone else only reads it.
- Using \`PlayerPrefs\` is simple and cross-platform, but you may prefer a per-account persistent store (cloud save) for cross-device consistency.

---

## Key Points
- \`Persistence\`: Player preferences are serialized with \`JsonUtility\` and saved to \`PlayerPrefs\`.
- \`Network Sync\`: \`NetworkList<int>\` replicates outfit variants to all clients and to late joiners.
- \`Owner Writes\`: Only the owner can change their list, preventing other clients from overriding someone else’s outfit.
- \`Immediate Feedback\`: Owner applies selection locally immediately for responsive UI while Netcode replicates the change.

---

## Wrap-up / Learnings

\`OutfitSelector.cs\` is a compact, practical pattern for per-player cosmetic state:
- Keep persisted state minimal (a list of indices).
- Use Netcode collections for automatic sync and late-joiner correctness.
- Provide immediate local changes for UX, rely on Netcode for eventual consistency.

If you’d like, I can:
- Add a small UI demo scene showing outfit cycling and saving.
- Replace \`PlayerPrefs\` with a cloud-save placeholder for cross-device profiles.
- Extract a reusable utility for saving/restoring arbitrary NetworkLists.

\`OutfitSelector.cs\` lives in your project and demonstrates how local persistence + Netcode collections make a simple, robust player customization workflow.
`,
};

