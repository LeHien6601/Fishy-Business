using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RoundTable : NetworkBehaviour
{
    [SerializeField] private Seat _seatPrefab;
    [SerializeField] private CardHolder _cardHolderPrefab;
    [SerializeField] private float _radius = 2.5f;
    private int _currentCapacity = 8;

    [SerializeField] private List<Seat> _seats;
    [SerializeField] private BoardManager _boardManager;
    private bool _gameplaying;

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
        _gameplaying = false;
        for (int i = 0; i < _seats.Count; i++)
        {
            float angle = i * Mathf.PI * 2 / _seats.Count;
            Vector3 seatPosition = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * _radius;
            Quaternion seatRotation = Quaternion.LookRotation(-seatPosition.normalized, Vector3.up);
            _seats[i].transform.SetLocalPositionAndRotation(transform.position + seatPosition, seatRotation);
            _seats[i].gameObject.SetActive(true);
        }
    }
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.L))
        {
            if(_gameplaying == false)
            {
                ArrangeSeats();
                _gameplaying = true;
            }
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

            var holder = Instantiate(_cardHolderPrefab, _seats[i].transform);
            holder.transform.localPosition = _cardHolderPrefab.transform.localPosition;
            holder.transform.localRotation = _cardHolderPrefab.transform.localRotation;
            _boardManager.GetCardHolder(holder);
        }
        _boardManager.StartGame();
    }

    [ContextMenu("Reset")]
    public void Reset() => InitSeats();
}