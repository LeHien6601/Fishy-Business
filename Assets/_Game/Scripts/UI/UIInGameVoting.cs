using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UIInGameVoting : UIView
{
    [Header("References")]
    [SerializeField] private List<UIVotingMember> _uiVotingMemebers = new();

    public override void Show()
    {
        UpdateUI();
        base.Show();
    }
    public override void Hide()
    {
        base.Hide();
    }
    void OnEnable()
    {
        
    }
    void OnDisable()
    {
        
    }
    private void UpdateUI()
    {
        //Set data for voting members in lobby
    }
    public void TriggerVoting(string id)
    {
        //Call gameplay manager to trigger voting by the current play to player has id id
    }
}
