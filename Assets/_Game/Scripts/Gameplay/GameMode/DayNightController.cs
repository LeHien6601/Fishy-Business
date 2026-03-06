using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class DayNightController : MonoBehaviour
{
    [SerializeField] private Image _coverImg;
    public void Cover()
    {
        Debug.Log("U r being covered, u cant see the board");
        _coverImg.gameObject.SetActive(true);
        _coverImg.DOFade(1, 0.5f).From(0);
    }

    public void Uncover()
    {
        Debug.Log("U r uncovered, u can see the board");
        _coverImg.gameObject.SetActive(false);
        _coverImg.DOFade(0, 0.5f).From(1);
    }
}