using UnityEngine;

public class UIView : MonoBehaviour
{
    public virtual void Show()
    {
        gameObject.SetActive(true);
    }
    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }
    public virtual void ShowWithParams(object param)
    {
        gameObject.SetActive(true);
    }
    public virtual void HideWithParams(object param)
    {
        gameObject.SetActive(false);
    }
}
