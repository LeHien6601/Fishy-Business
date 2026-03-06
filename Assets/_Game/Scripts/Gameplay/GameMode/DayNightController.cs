using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class DayNightController : MonoBehaviour
{
    [SerializeField] private Image _coverImg;
    [SerializeField] private List<Image> _stars;
    public void Cover()
    {
        Debug.Log("U r being covered, u cant see the board");
        _coverImg.gameObject.SetActive(true);
        _coverImg.DOFade(1, 0.5f).From(0);
        foreach (var star in _stars)
        {
            star.gameObject.SetActive(true);
            star.transform.DORotate(Vector3.right * 30f, 0.2f).SetLoops(-1, LoopType.Yoyo);
        }
    }

    public void Uncover()
    {
        Debug.Log("U r uncovered, u can see the board");
        _coverImg.DOFade(0, 0.5f).From(1).OnComplete(() => _coverImg.gameObject.SetActive(false));
        foreach (var star in _stars)
        {
            star.gameObject.SetActive(false);
            star.transform.DOKill();
        }
    }
}