// On the NetworkObject component attached to the piano or player
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class InstrumentController : NetworkBehaviour
{
    [SerializeField] private InstrumentInput _musicInput;
    [SerializeField] private float _maxRecordingDuration = 1f; // in seconds
    [SerializeField] private int _maxNotesInSequence = 100;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private MusicNotesDatabaseSO _musicNotesDatabase;
    [SerializeField] private Seat _seat;
    private float _recordingStartTime;
    private List<NetNote> _noteBuffer;


    void OnEnable()
    {
        _seat.OnLocalSeatChanged += OnSeatChanged;
    }

    private void OnDisable()
    {
        _musicInput.OnNotePlayed -= HandleNotePlayed;
    }

    private void OnSeatChanged(bool arg0)
    {
        if (arg0)
        {
            StartInstrument();
        }
        else
        {
            StopInstrument();
        }
    }

    private void StopInstrument()
    {
        StopAllCoroutines();
        _musicInput.OnNotePlayed -= HandleNotePlayed;
        _musicInput.gameObject.SetActive(false);
    }

    public void StartInstrument()
    {
        _musicInput.gameObject.SetActive(true);
        _musicInput.OnNotePlayed += HandleNotePlayed;
        _noteBuffer = new List<NetNote>();
        StartCoroutine(PollInput());
    }

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

    private void HandleNotePlayed(int note)
    {
        if (_noteBuffer.Count == 0)
        {
            _recordingStartTime = Time.time;
        }

        // Add note to buffer
        _noteBuffer.Add(new NetNote
        {
            InstrumentID = 0, // Assuming single instrument for now
            NoteID = (byte)note,
            RelativeTime = (int)((Time.time - _recordingStartTime) * 1000) // in milliseconds
        });
        if (_noteBuffer.Count >= _maxNotesInSequence)
        {
            // Send sequence to server
            SendNoteSequenceServerRpc(_noteBuffer.ToArray(), NetworkManager.Singleton.LocalClientId);
            _noteBuffer.Clear();
        }

        // Play note locally
        _audioSource.PlayOneShot(_musicNotesDatabase.GetAudioClipForNote(note));
    }


    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SendNoteSequenceServerRpc(NetNote[] sequence, ulong senderId)
    {
        List<ulong> targets = NetworkManager.Singleton.ConnectedClientsIds.ToList();
        targets.Remove(senderId);
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = targets.ToArray() // Send to all clients except sender
            }
        };
        PlayNoteSequenceOtherClientRpc(sequence, clientRpcParams);
    }

    [ClientRpc]
    private void PlayNoteSequenceOtherClientRpc(NetNote[] sequence, ClientRpcParams clientRpcParams)
    {
        StartCoroutine(StartPlayback(sequence));
    }


    private IEnumerator StartPlayback(NetNote[] sequence)
    {
        float playbackStartTime = Time.time;
        int noteIndex = 0;

        while (noteIndex < sequence.Length)
        {
            NetNote note = sequence[noteIndex];

            // Calculate the target time for this note in the global timeline
            float targetTime = playbackStartTime + (note.RelativeTime / 1000f);

            // Wait until the target time is reached
            float timeToWait = targetTime - Time.time;

            if (timeToWait > 0)
            {
                yield return new WaitForSeconds(timeToWait);
            }

            // Play the note
            _audioSource.PlayOneShot(_musicNotesDatabase.GetAudioClipForNote(note.NoteID));
            noteIndex++;

        }
    }
}