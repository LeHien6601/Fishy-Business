
import imageImport from "@/assets/instrument-controller.png";
import { DevlogEntry } from "@/data/devlog";

export const templateDevlog: DevlogEntry = {
  id: "instrumentcontroller-batching-2026-05-03",
  title: "InstrumentController: Batching Notes to Avoid Network Flood",
  teaser: "How PollInput batches note events to prevent network spam while keeping local responsiveness.",
  image: imageImport,
  date: "2026-05-03",
  content: `# InstrumentController: Batching Notes to Avoid Network Flood

## Challenge

Sending a network event for every key press floods the network and can hurt performance and bandwidth.

## Solution: PollInput batching

The controller collects note events locally into a buffer and periodically sends the accumulated sequence to the server. Local playback remains immediate so the player gets instant feedback.

Key snippets:

\`\`\`csharp
private IEnumerator PollInput()
{
	while (true)
	{
		yield return Utils.GetWaitForSeconds(_maxRecordingDuration);
		if (_noteBuffer.Count > 0)
		{
			// Send sequence to server
			SendNoteSequenceServerRpc(_noteBuffer.ToArray(), NetworkManager.Singleton.LocalClientId);
			_noteBuffer.Clear();
		}
	}
}
\`\`\`

Local collection and immediate local feedback:

\`\`\`csharp
private void HandleNotePlayed(int note)
{
	if (_noteBuffer.Count == 0)
		_recordingStartTime = Time.time;

	_noteBuffer.Add(new NetNote
	{
		InstrumentID = 0,
		NoteID = (byte)note,
		RelativeTime = (int)((Time.time - _recordingStartTime) * 1000)
	});

	if (_noteBuffer.Count >= _maxNotesInSequence)
	{
		SendNoteSequenceServerRpc(_noteBuffer.ToArray(), NetworkManager.Singleton.LocalClientId);
		_noteBuffer.Clear();
	}

	// Play note locally for instant feedback
	_audioSource.PlayOneShot(_musicNotesDatabase.GetAudioClipForNote(note));
}
\`\`\`

Server RPC that rebroadcasts to other clients (sender excluded):

\`\`\`csharp
[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
public void SendNoteSequenceServerRpc(NetNote[] sequence, ulong senderId)
{
	List<ulong> targets = NetworkManager.Singleton.ConnectedClientsIds.ToList();
	targets.Remove(senderId);
	ClientRpcParams clientRpcParams = new ClientRpcParams
	{
		Send = new ClientRpcSendParams
		{
			TargetClientIds = targets.ToArray()
		}
	};
	PlayNoteSequenceOtherClientRpc(sequence, clientRpcParams);
}
\`\`\`

Playback on other clients reconstructs timing using the recorded relative timestamps.

## Short takeaways
- Batch inputs with a short polling window to reduce RPC traffic.
- Keep local playback immediate for good UX.
- Include relative timestamps so playback stays in sync when rebroadcast.

`,
};

