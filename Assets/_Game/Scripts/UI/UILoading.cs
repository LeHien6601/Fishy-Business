using TMPro;
using UnityEngine;

public class UILoading : UIView
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _loadingText;

    private async void OnEnable()
    {
        _loadingText.text = "Loading";
        int n = 0;
        while (true)
        {
            n = (n + 1) % 4;
            _loadingText.text = "Loading" + new string('.', n);
            await System.Threading.Tasks.Task.Delay(300);
            if (!IsShowing) break;
        }
    }
}
