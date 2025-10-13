using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RoundTable : NetworkBehaviour
{
    [SerializeField] private Seat _seatPrefab;
    [SerializeField] private float _radius = 2.5f;
    private int _currentCapacity = 8;

    [SerializeField] private List<Seat> _seats;


    public override void OnNetworkSpawn()
    {
        if (!IsHost)
        {
            return;
        }
        for (int i = 0; i < _currentCapacity; i++)
        {
            var seat = Instantiate(_seatPrefab);
            seat.GetComponent<NetworkObject>().Spawn(true);
            seat.name = $"Seat {i + 1}";
            _seats.Add(seat);
        }
        InitSeats();
    }


    private void InitSeats()
    {
        for (int i = 0; i < _seats.Count; i++)
        {
            float angle = i * Mathf.PI * 2 / _seats.Count;
            Vector3 seatPosition = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * _radius;
            Quaternion seatRotation = Quaternion.LookRotation(-seatPosition.normalized, Vector3.up);
            _seats[i].transform.SetLocalPositionAndRotation(transform.position + seatPosition, seatRotation);
            _seats[i].gameObject.SetActive(true);
        }
    }

    // @TODO: ease the animation
    [ContextMenu("ArrangeSeats")]
    private void ArrangeSeats()
    {
        int _currentCapacity = _seats.FindAll(seat => seat.IsOccupied()).Count;
        for (int i = 0; i < _seats.Count; i++)
        {
            if (!_seats[i].IsOccupied())
            {
                _seats[i].gameObject.SetActive(false);
                continue;
            }
            float angle = i * Mathf.PI * 2 / _currentCapacity;
            Vector3 seatPosition = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (_radius - 0.5f);
            Quaternion seatRotation = Quaternion.LookRotation(-seatPosition.normalized, Vector3.up);
            _seats[i].transform.SetLocalPositionAndRotation(transform.position + seatPosition, seatRotation);
            _seats[i].gameObject.SetActive(true);
        }
    }

    [ContextMenu("Reset")]
    public void Reset() => InitSeats();
}