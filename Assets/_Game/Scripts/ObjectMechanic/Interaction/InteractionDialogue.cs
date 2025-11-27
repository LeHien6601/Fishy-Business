using DG.Tweening;
using TMPro;
using UnityEngine;

public class InteractionDialogue : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _dialogueText;

    public void Show()
    {
        transform.localScale = Vector3.zero;
        transform.DOScale(1, 0.2f).SetEase(Ease.OutBack);
    }

    public void SetDialogue(string dialogue)
    {
        _dialogueText.text = dialogue;

        transform.localScale = Vector3.zero;
        transform.DOScale(1, 0.2f).SetEase(Ease.OutBack);
    }

    public void ClearDialogue()
    {
        // _dialogueText.text = "";
        transform.DOScale(0, 0.2f).SetEase(Ease.InBack);
    }
}