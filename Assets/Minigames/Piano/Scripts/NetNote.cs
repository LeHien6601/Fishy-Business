using Unity.Netcode;
using UnityEngine;

public struct NetNote : INetworkSerializable
{
    // The MIDI note number (0-127) or another simple ID for the key
    public byte InstrumentID;
    public byte NoteID; 
    
    // The time *relative* to the start of the current recording/sequence.
    // Use a high-precision type like double or a large integer for milliseconds.
    public  int RelativeTime; 

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref InstrumentID);
        serializer.SerializeValue(ref NoteID);
        serializer.SerializeValue(ref RelativeTime);
    }
}