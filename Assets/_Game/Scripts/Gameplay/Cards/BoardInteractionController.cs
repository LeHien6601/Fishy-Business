using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class BoardInteractionController
{
    private readonly NetworkBoardManager _owner;

    public BoardInteractionController(NetworkBoardManager owner)
    {
        _owner = owner;
    }

    public Card PlacingCard { get; set; }
    public Vector2Int? HoveringSlot { get; set; }
    public ulong? TargetPlayer { get; set; }
    public PlayerState LocalPlayerState { get; private set; } = PlayerState.NONE;

    public void HandleUpdate()
    {
        switch (LocalPlayerState)
        {
            case PlayerState.PLACING_CARD:
                HoverCardOnBoard(_owner.BoardCore.ValidSlots);
                if (!HoveringSlot.HasValue) return;

                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    LocalPlayerState = PlayerState.NONE;
                    _owner.RequestConfirmPlacement(HoveringSlot.Value, PlacingCard.GetRealRotation());
                }

                if (Mouse.current.rightButton.wasPressedThisFrame &&
                    _owner.BoardCore.IsPlacableWithOppositeRotation(PlacingCard, HoveringSlot.Value))
                {
                    _owner.RequestRotatePlacingCard();
                }
                break;

            case PlayerState.SELECTING_PLACE_TO_BOMB:
                HoverCardOnBoard(_owner.BoardCore.OnBoardPaths);
                if (!HoveringSlot.HasValue) return;

                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    LocalPlayerState = PlayerState.NONE;
                    _owner.RequestBombPath(HoveringSlot.Value, NetworkManager.Singleton.LocalClientId);
                }
                break;

            case PlayerState.SELECTING_TARGET_PLAYER:
                HoverCurrentActionTarget();
                if (!TargetPlayer.HasValue) return;

                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    LocalPlayerState = PlayerState.NONE;
                    _owner.RequestApplyActionToTarget(TargetPlayer.Value, NetworkManager.Singleton.LocalClientId);
                }
                break;

            case PlayerState.CHECKING_GOAL:
                HoverCardOnBoard(_owner.BoardCore.GoalPos);
                if (!HoveringSlot.HasValue) return;

                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    LocalPlayerState = PlayerState.NONE;
                    _owner.RequestCheckGoal(NetworkManager.Singleton.LocalClientId, HoveringSlot.Value);
                }
                break;

            case PlayerState.SWAPING_GOALS:
                HoverCardOnBoard(_owner.BoardCore.GoalPos);
                if (!HoveringSlot.HasValue) return;

                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    LocalPlayerState = PlayerState.NONE;
                    int slotX = ((HoveringSlot.Value.x / 2 + 4) % 3) * 2;
                    _owner.RequestSwapGoals(HoveringSlot.Value, new Vector2Int(slotX, HoveringSlot.Value.y), NetworkManager.Singleton.LocalClientId);
                }
                else if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    LocalPlayerState = PlayerState.NONE;
                    int slotX = ((HoveringSlot.Value.x / 2 + 2) % 3) * 2;
                    _owner.RequestSwapGoals(HoveringSlot.Value, new Vector2Int(slotX, HoveringSlot.Value.y), NetworkManager.Singleton.LocalClientId);
                }
                break;

            case PlayerState.NONE:
            default:
                break;
        }
    }

    public void BeginCardInteraction(Card card)
    {
        PlacingCard = card;
        HoveringSlot = null;
        TargetPlayer = null;
    }

    public void EnterStateForCard(Card card)
    {
        LocalPlayerState = MatchStateWithCard(card);
    }

    public void SetIdle()
    {
        LocalPlayerState = PlayerState.NONE;
    }

    public void ClearPlacement()
    {
        PlacingCard = null;
        HoveringSlot = null;
    }

    public void ClearTarget()
    {
        TargetPlayer = null;
    }

    public void ClearCardAndTarget()
    {
        PlacingCard = null;
        HoveringSlot = null;
        TargetPlayer = null;
    }

    public void Reset()
    {
        PlacingCard = null;
        HoveringSlot = null;
        TargetPlayer = null;
        LocalPlayerState = PlayerState.NONE;
    }

    private void HoverCurrentActionTarget()
    {
        switch (PlacingCard.ActionCardType)
        {
            case ActionCardType.BrokenTool:
                HoverTargetPlayer(player => player.IsMine || !player.HasTool(PlacingCard.ToolType));
                break;
            case ActionCardType.FixTool:
                HoverTargetPlayer(player => player.HasTool(PlacingCard.ToolType));
                break;
            case ActionCardType.Binoculars:
            case ActionCardType.Shield:
                HoverTargetPlayer(_ => false);
                break;
        }
    }

    private void HoverCardOnBoard(List<Vector2Int> slots)
    {
        Vector3 mouseWorld = _owner.GetMouseWorldPointOnBoard();
        if (mouseWorld == Vector3.zero)
        {
            return;
        }

        float sqrDistance = 1f;
        Vector2Int? bestSlot = null;
        foreach (var slot in slots)
        {
            Vector3 worldPosition = _owner.BoardCore.GetWorldPositionForSlot(slot);
            float currentDistance = Vector3.SqrMagnitude(mouseWorld - worldPosition);
            if (currentDistance < sqrDistance)
            {
                sqrDistance = currentDistance;
                bestSlot = slot;
            }
        }

        if (bestSlot.HasValue && (!HoveringSlot.HasValue || HoveringSlot.Value != bestSlot.Value))
        {
            HoveringSlot = bestSlot.Value;
            _owner.RequestMovePlacingCard(bestSlot.Value, PlacingCard.GetRealRotation());
        }
    }

    private void HoverTargetPlayer(Func<CardHolder, bool> ignore)
    {
        Vector3 mouseWorld = _owner.GetMouseWorldPointOnBoard();
        if (mouseWorld == Vector3.zero)
        {
            return;
        }

        float sqrDistance = float.MaxValue;
        ulong? closestPlayer = null;
        foreach (var player in _owner.PlayerAndHolderMap)
        {
            if (ignore(player.Value))
            {
                continue;
            }

            Vector3 worldPosition = player.Value.transform.position;
            float currentDistance = Vector3.SqrMagnitude(mouseWorld - worldPosition);
            if (currentDistance < sqrDistance)
            {
                sqrDistance = currentDistance;
                closestPlayer = player.Key;
            }
        }

        if (closestPlayer.HasValue && (!TargetPlayer.HasValue || TargetPlayer.Value != closestPlayer.Value))
        {
            TargetPlayer = closestPlayer.Value;
            _owner.RequestSwitchTargetPlayer(TargetPlayer.Value);
        }
    }

    private static PlayerState MatchStateWithCard(Card card)
    {
        if (card.CardType == CardType.Path)
        {
            return PlayerState.PLACING_CARD;
        }

        if (card.CardType == CardType.Action)
        {
            return card.ActionCardType switch
            {
                ActionCardType.CheckGold => PlayerState.CHECKING_GOAL,
                ActionCardType.Bomb => PlayerState.SELECTING_PLACE_TO_BOMB,
                ActionCardType.FixTool => PlayerState.SELECTING_TARGET_PLAYER,
                ActionCardType.BrokenTool => PlayerState.SELECTING_TARGET_PLAYER,
                ActionCardType.Binoculars => PlayerState.SELECTING_TARGET_PLAYER,
                ActionCardType.Shield => PlayerState.SELECTING_TARGET_PLAYER,
                ActionCardType.SwapGoal => PlayerState.SWAPING_GOALS,
                _ => PlayerState.NONE
            };
        }

        return PlayerState.NONE;
    }
}

public enum PlayerState
{
    NONE,
    PLACING_CARD,
    SELECTING_PLACE_TO_BOMB,
    SELECTING_TARGET_PLAYER,
    CHECKING_GOAL,
    SWAPING_GOALS,
}
