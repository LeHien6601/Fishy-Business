using TMPro;
using UnityEngine;

public class UITextChatElement : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _displayTMP;
    public void SetText(string text)
    {
        _displayTMP.text = text;
    }
}