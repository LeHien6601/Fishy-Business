using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    #region Properties
    [Header("Animations")]
    [SerializeField] private UIAnimation _enterAnimation;
    [SerializeField] private UIAnimation _exitAnimation;
    [SerializeField] private UIAnimation _clickAnimation;
    [SerializeField] private UIAnimation _globalAnimation;
    [SerializeField] public int EnterPresetIndex = -1;
    [SerializeField] public int ExitPresetIndex = -1;
    [SerializeField] public int ClickPresetIndex = -1;

    private Button _button;
    #endregion
    void Awake()
    {
        if (_button == null) _button = GetComponent<Button>();
    }

    #region Mouse Events
    public async void OnPointerEnter(PointerEventData eventData)
    {
        if (_enterAnimation) await _enterAnimation.PlayAnimation();
        else if (EnterPresetIndex != -1) PlayGlobalAnimation(EnterPresetIndex);
    }

    public async void OnPointerExit(PointerEventData eventData)
    {
        if (_exitAnimation) await _exitAnimation.PlayAnimation();
        else if (ExitPresetIndex != -1) PlayGlobalAnimation(ExitPresetIndex);
    }

    public async void OnPointerClick(PointerEventData eventData)
    {
        if (_clickAnimation) await _clickAnimation.PlayAnimation();
        else if (ClickPresetIndex != -1) PlayGlobalAnimation(ClickPresetIndex);
    }
    #endregion

    private async void PlayGlobalAnimation(int index)
    {
        if (_globalAnimation)
        {
            _globalAnimation.SetAnimation(UIAnimationType.Button, index);
            await _globalAnimation.PlayAnimation();
        }
    }
}
