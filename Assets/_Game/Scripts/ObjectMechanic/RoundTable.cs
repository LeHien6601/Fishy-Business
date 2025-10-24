using System.Collections.Generic;
using DG.Tweening;
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
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (_gameplaying == false)
            {
                ArrangeSeats();
                _gameplaying = true;
            }
        }
    }

    /*
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
    */


    // @TODO: ease the animation
    [ContextMenu("ArrangeSeats")]
    private void ArrangeSeats()
    {
        // 1. Count occupied seats
        int occupiedCount = _seats.FindAll(s => s.IsOccupied()).Count;

        // 2. We'll store the tween sequences for each seat so we can wait for all of them
        List<Tween> tweens = new List<Tween>();

        for (int i = 0; i < _seats.Count; i++)
        {
            if (!_seats[i].IsOccupied())
            {
                _seats[i].gameObject.SetActive(false);
                continue;
            }

            // ----- calculate target position / rotation (exactly like your original code) -----
            float angle = i * Mathf.PI * 2f / occupiedCount;
            Vector3 targetPos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (_radius - 0.5f);
            Quaternion targetRot = Quaternion.LookRotation(-targetPos.normalized, Vector3.up);
            targetPos += transform.position;                  // world-world space

            Transform seatTf = _seats[i].transform;

            // hide while we prepare the holder (optional – you can keep it visible)
            _seats[i].gameObject.SetActive(true);

            // ----- instantiate the card holder *before* moving (so it follows the seat) -----
            var holder = Instantiate(_cardHolderPrefab, seatTf);
            holder.transform.SetLocalPositionAndRotation(_cardHolderPrefab.transform.localPosition, _cardHolderPrefab.transform.localRotation);
            _boardManager.GetCardHolder(holder);

            // ----- DOTween animation -----
            float duration = 0.8f;
            Ease easeType = Ease.OutCubic;      // smooth deceleration

            // 1) Position tween
            Tween posTween = seatTf.DOMove(targetPos, duration)
                                   .SetEase(easeType);

            // 2) Rotation tween (runs in parallel)
            Tween rotTween = seatTf.DOLocalRotateQuaternion(targetRot, duration)
                                   .SetEase(easeType);

            // Combine them into a single Sequence so we can track completion
            Sequence seatSeq = DOTween.Sequence();
            seatSeq.Append(posTween);
            seatSeq.Join(rotTween);   // runs at the same time as position

            tweens.Add(seatSeq);
        }

        // ----- When *all* seats have finished moving, start the game -----
        if (tweens.Count > 0)
        {
            // Create a dummy tween that completes when every seat tween is done
            Sequence allDone = DOTween.Sequence();
            foreach (var t in tweens) allDone.Join(t);

            allDone.OnComplete(() => _boardManager.StartGame());
        }
        else
        {
            // No occupied seats → start immediately
            _boardManager.StartGame();
        }
    }

    [ContextMenu("Reset")]
    public void Reset() => InitSeats();
}