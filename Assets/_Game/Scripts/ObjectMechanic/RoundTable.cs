using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class RoundTable : NetworkBehaviour
{
    [SerializeField] private Seat _seatPrefab;
    [SerializeField] private CardHolder _cardHolderPrefab;
    [SerializeField] private float _radius = 2.5f;
    private readonly int _currentCapacity = 8;

    [SerializeField] private NetworkBoardManager _boardManager; // local version, each has 1. 
    private readonly NetworkVariable<NetworkObjectReference> _boardManagerRef =
    new(new NetworkObjectReference(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); // network version

    [SerializeField] private List<Seat> _seats; // only server knows this list
    private readonly List<NetworkObjectReference> _netSeats = new(); // all clients know this list
    public NetworkList<ulong> PlayerOrders = new(); // server writes, all read
    [Header("UI References")]
    [SerializeField] private Button _startBtn;
    [SerializeField] private UIRoundTable _uiRoundTable;
    [SerializeField] private TextMeshProUGUI _countdownTMP;

    private NetworkVariable<bool> _gameplaying = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private Coroutine _startGameCoroutine;
    public override async void OnNetworkSpawn()
    {
        if (IsHost || IsServer)
        {
            while (_boardManager != null && !_boardManager.NetworkObject.IsSpawned)
            {
                await Task.Yield(); 
            }

            if (_boardManager != null)
            {
                _boardManagerRef.Value = new NetworkObjectReference(_boardManager.NetworkObject);
                Debug.Log("Server set BoardManager reference.");
            }
            for (int i = 0; i < _currentCapacity; i++)
            {
                Debug.Log("INSTANTIATE SEAT");
                var seat = Instantiate(_seatPrefab);
                seat.GetComponent<NetworkObject>().Spawn(true);
                seat.name = $"Seat {i + 1}";
                _seats.Add(seat);
                seat.OnPlayerEnterSeat += HandleChangeGameStateRpc;
            }
            InitSeats();
            GameplayManager.Instance.OnResetGame += ResetServerRpc;
            LobbyManager.Instance.OnPlayerLeftLobby += HandlePlayerLeaveLobby;
        }
        _startBtn.onClick.AddListener(StartBoardGameServerRpc);
        _gameplaying.OnValueChanged += HandleChangeGameState;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        GameplayManager.Instance.OnResetGame -= ResetServerRpc;
        _startBtn.onClick.RemoveAllListeners();
        _gameplaying.OnValueChanged -= HandleChangeGameState;
        LobbyManager.Instance.OnPlayerLeftLobby -= HandlePlayerLeaveLobby;
    }
    [Rpc(SendTo.Everyone)]
    private void HandleChangeGameStateRpc(Seat.PlayerEnterSeatEventArg args)
    {
        if (args.OldClientId == NetworkManager.Singleton.LocalClientId)
        {
            _uiRoundTable.gameObject.SetActive(false);
        }
        if (args.NewClientId == NetworkManager.Singleton.LocalClientId)
        {
            _uiRoundTable.gameObject.SetActive(true);
        }
    }

    private void HandleChangeGameState(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            _uiRoundTable.gameObject.SetActive(false);
            HandleCountdownTimer();
        }
        else
        {
            _uiRoundTable.gameObject.SetActive(true);
            _countdownTMP.gameObject.SetActive(false);
        }
    }
    private async void HandleCountdownTimer()
    {
        _countdownTMP.gameObject.SetActive(true);
        _countdownTMP.text = "GAME STARTS IN " + Constant.START_GAME_COUNTDOWN.ToString("F0") +"S";
        float timer = Constant.START_GAME_COUNTDOWN;
        while (timer > 0 && _gameplaying.Value)
        {
            await Task.Yield();
            timer -= Time.deltaTime;
            _countdownTMP.text = "GAME STARTS IN " + Mathf.Ceil(timer).ToString("F0") +"S";
        }
        if (_countdownTMP && _countdownTMP.gameObject) _countdownTMP.gameObject.SetActive(false);
    }

    private void HandlePlayerLeaveLobby(LobbyManager.PlayerLeftLobbyEventArgs args)
    {
        if (GameManager.Instance.GetNetIdByAuthId(args.AuthId, out ulong playerId))
        {
            foreach (var seat in _seats)
            {
                if (seat && seat.GetOccupyingClientId() == playerId)
                {
                    Debug.Log(args.AuthId + " " + playerId);
                    seat.ServerEmptySeat();
                }
            }
        }
        ResetServerRpc();
    }

    public override void OnDestroy()
    {
        _startBtn.onClick.RemoveAllListeners();
        base.OnDestroy();
    }

    private void InitSeats()
    {
        _netSeats.Clear();
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

    /// <summary>
    /// one player calls this and starts game across all clients
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void StartBoardGameServerRpc()
    {
        if (_gameplaying.Value)
            return;
        if (_boardManager == null)
        {
            Debug.LogError("Server BoardManager reference is missing!");
            return;
        }
        SoundManager.Play2D(SoundType.StartGame);
        _gameplaying.Value = true;

        PlayerOrders.Clear();
        int occupiedCount = 0;
        foreach (var seat in _seats)
        {
            if (seat.IsOccupied())
            {
                PlayerOrders.Add(seat.GetOccupyingClientId());
                GameplayManager.Instance.TriggerStartGame(seat.GetOccupyingClientId());
                occupiedCount++;
            }
        }
        // this.WaitThenExecute(5f, () =>
        // {
        //     ArrangeSeatsClientRpc(_netSeats.ToArray(), occupiedCount);
        //     _boardManager.ServerStartGameLogic(PlayerOrders);
        // });
        if (_startGameCoroutine != null)
        {
            StopCoroutine(_startGameCoroutine);
        }
        _startGameCoroutine = StartCoroutine(StartGameAfterDelay(_netSeats.ToArray(), occupiedCount, PlayerOrders));
    }
    private IEnumerator StartGameAfterDelay(NetworkObjectReference[] netSeats, int occupiedCount, NetworkList<ulong> playerOrders)
    {
        yield return new WaitForSeconds(5f);
        if (!_gameplaying.Value)
        {
            yield break;
        }
        ArrangeSeatsClientRpc(netSeats, occupiedCount);
        _boardManager.ServerStartGameLogic(playerOrders); 
    }


    /// <summary>
    /// handle seat arrangement animation on all clients locally
    /// spawn local cardHolders for each occupied seat
    /// </summary>
    /// <param name="seatRefs"></param>
    /// <param name="occupiedCount"></param>
    [ContextMenu("ArrangeSeats")]
    [ClientRpc]
    private void ArrangeSeatsClientRpc(NetworkObjectReference[] seatRefs, int occupiedCount)
    {
        int i = 0;
        foreach (var seatRef in seatRefs)
        {
            Seat seat = seatRef.TryGet(out NetworkObject netObj) ? netObj.GetComponent<Seat>() : null;
            if (!seat)
            {
                Debug.LogWarning("Seat Component not found in a seat");
                continue;
            }
            if (!seat.IsOccupied())
            {
                seat.gameObject.SetActive(false);
                continue;
            }

            // ----- calculate target position / rotation -----
            float angle = i++ * Mathf.PI * 2f / occupiedCount;
            Vector3 targetPos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (_radius - 0.5f);
            Quaternion targetRot = Quaternion.LookRotation(-targetPos.normalized, Vector3.up);
            targetPos += transform.position;                  // world-world space

            Transform seatTf = seat.transform;

            // ----- instantiate the card holder *before* moving (so it follows the seat) -----
            var holder = Instantiate(_cardHolderPrefab, seatTf);
            holder.transform.SetLocalPositionAndRotation(_cardHolderPrefab.transform.localPosition, _cardHolderPrefab.transform.localRotation);
            _boardManager.RegisterCardHolder(seat.GetOccupyingClientId(), holder);
            if (seat.GetOccupyingClientId() == NetworkManager.Singleton.LocalClientId)
            {
                holder.IsMine = true;
            }

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
                Tween occupantPosTween = occupantTf.DOMove(seat.SitPosition(targetPos, targetRot), duration)
                                       .SetEase(easeType);
                Tween occupantRotTween = occupantTf.DOLocalRotateQuaternion(targetRot, duration)
                                       .SetEase(easeType);
            }
        }
    }

    [ContextMenu("Reset")]
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ResetServerRpc()
    {
        if (!_gameplaying.Value)
            return;

        _gameplaying.Value = false;
        _boardManager.Reset();
        foreach (var seat in _seats)
        {
            GameplayManager.Instance.TriggerEndGame(seat.GetOccupyingClientId());
        }
        ResetSeatsClientRpc(_netSeats.ToArray());
    }

    [ClientRpc]
    private void ResetSeatsClientRpc(NetworkObjectReference[] seatRefs)
    {

        for (int i = 0; i < seatRefs.Length; i++)
        {
            Seat seat = seatRefs[i].TryGet(out NetworkObject netObj) ? netObj.GetComponent<Seat>() : null;
            seat.gameObject.SetActive(true);
            // ----- calculate target position / rotation -----
            float angle = i * Mathf.PI * 2f / seatRefs.Length;
            Vector3 targetPos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (_radius - 0.5f);
            Quaternion targetRot = Quaternion.LookRotation(-targetPos.normalized, Vector3.up);
            targetPos += transform.position;                  // world-world space

            Transform seatTf = seat.transform;
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
                if (NetworkManager.Singleton.LocalClientId == seat.GetOccupyingClientId())
                {
                    _uiRoundTable.gameObject.SetActive(true);
                }
                Transform occupantTf = seat.GetOccupant().transform;
                Tween occupantPosTween = occupantTf.DOMove(seat.SitPosition(targetPos, targetRot), duration)
                                       .SetEase(easeType);
                Tween occupantRotTween = occupantTf.DOLocalRotateQuaternion(targetRot, duration)
                                       .SetEase(easeType);
            }
        }
    }
}

