using System.Collections.Generic;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class RoundTable : NetworkBehaviour
{
    [SerializeField] private Seat _seatPrefab;
    [SerializeField] private CardHolder _cardHolderPrefab;
    [SerializeField] private float _radius = 2.5f;
    private readonly int _currentCapacity = 8;

    [SerializeField] private BoardManager _boardManager;
    [SerializeField] private List<Seat> _seats; // only server knows this list
    private readonly List<NetworkObjectReference> _netSeats = new(); // all clients know this list
    public NetworkList<ulong> PlayerOrders = new(); // server writes, all read
    private bool _gameplaying;
    public event UnityAction OnBoardGameStarted;
    public event UnityAction OnTurnEnded;
    public event UnityAction OnBoardGameEnded;
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
            _netSeats.Add(new NetworkObjectReference(_seats[i].NetworkObject));
        }
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (_gameplaying == false)
            {
                StartBoardGameServerRpc();
                _gameplaying = true;
            }
        }
    }

    /// <summary>
    /// one player calls this and starts game across all clients
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void StartBoardGameServerRpc()
    {
        PlayerOrders.Clear();
        int occupiedCount = 0;
        foreach (var seat in _seats)
        {
            if (seat.IsOccupied())
            {
                PlayerOrders.Add(seat.GetOccupyingClientId());
                occupiedCount++;
            }
        }
        ArrangeSeatsClientRpc(_netSeats.ToArray(), occupiedCount);
    }

    // @TODO: ease the animation
    [ContextMenu("ArrangeSeats")]
    [ClientRpc]
    private void ArrangeSeatsClientRpc(NetworkObjectReference[] seatRefs, int occupiedCount)
    {
        // 1. Count occupied seats
        List<Tween> tweens = new List<Tween>();

        for (int i = 0; i < seatRefs.Length; i++)
        {
            Seat seat = seatRefs[i].TryGet(out NetworkObject netObj) ? netObj.GetComponent<Seat>() : null;
            if (!seat || !seat.IsOccupied())
            {
                seat.gameObject.SetActive(false);
                continue;
            }

            // ----- calculate target position / rotation (exactly like your original code) -----
            float angle = i * Mathf.PI * 2f / occupiedCount;
            Vector3 targetPos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (_radius - 0.5f);
            Quaternion targetRot = Quaternion.LookRotation(-targetPos.normalized, Vector3.up);
            targetPos += transform.position;                  // world-world space

            Transform seatTf = seat.transform;

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

            if (seat.GetOccupant())
            {
                Transform occupantTf = seat.GetOccupant().transform;
                Tween occupantTween = occupantTf.DOMove(targetPos, duration)
                                       .SetEase(easeType);
                Tween occupantRotTween = occupantTf.DOLocalRotateQuaternion(targetRot, duration)
                                       .SetEase(easeType);
            }

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