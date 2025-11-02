using TMPro;
using UnityEngine;

public class UIPlayerEndGame : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIIconItem _uiIconItem;
    [SerializeField] private TextMeshProUGUI _nameTMP;

    public void UpdatePlayerInfo(string name, int iconId)
    {
        _uiIconItem.SetIcon(iconId);
        _nameTMP.text = name;
    }
}
