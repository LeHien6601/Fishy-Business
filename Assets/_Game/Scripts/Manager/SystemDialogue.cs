using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class SystemDialogue : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _dialogueText;
    [SerializeField] private GameObject _textBox;

    private static event UnityAction<string, float> OnSetDialogue;
    private void OnEnable()
    {
        OnSetDialogue += SetDialogue_Internal;
    }
    private void OnDisable()
    {
        OnSetDialogue -= SetDialogue_Internal;
    }
    public static void SetDialogue(string dialogue, float existTime = -1f)
    {
        OnSetDialogue?.Invoke(dialogue, existTime);
    }
    public static void ClearDialogue()
    {
        OnSetDialogue?.Invoke(string.Empty, -1f);
    }

    private void SetDialogue_Internal(string dialogue, float existTime)
    {
        if (string.IsNullOrEmpty(dialogue))
        {
            // _dialogueText.text = "";
            _textBox.SetActive(false);
            // _textBox.transform.DOScale(0, 0.2f).SetEase(Ease.InBack);
        }
        else
        {
            _textBox.SetActive(true);
            _dialogueText.text = dialogue;
            _textBox.transform.localScale = Vector3.zero;
            _textBox.transform.DOScale(1, 0.2f).SetEase(Ease.OutBack);
        }
        if (existTime > 0)
        {
            DOVirtual.DelayedCall(existTime, () => ClearDialogue());
        }
    }
}