//This is a crucial step in building a server-authoritative game. 
// To ensure the card-dealing process is fair and synchronized, we must use a Server-to-Client (Targeted ClientRpc) communication pattern.
// Here is the structured solution, focusing on passing card data from the authoritative $\text{BoardManager}$ (Server) to the correct player's local machine (Client).1.
// Define the Card DataSince all clients need to know what card they have, we must define the data structure that represents a card over the network. 
// Using an integer ID that maps to a global Card Data scriptable object or master list is the most performant method.
// C#// 🃏 A simple structure for card data (e.g., in a separate file or BoardManager)
// Use an int ID to reference the actual card definition (e.g., "Attack Card," "Heal Card").
using Unity.Netcode;

public struct CardData : INetworkSerializable
{
    // only CardID for now, we can plan to expand later
    public int CardID;

    // Required method for Network serialization
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref CardID);
    }
}