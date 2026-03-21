using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class DayNightController : MonoBehaviour
{
    [Header("-----------Used Item-----------")]
    [SerializeField] private Mask _mask;
    [SerializeField] private Image _coverUsedItemImg;

    [Header("-------------Normal-------------")]
    [SerializeField] private GameObject _coverObject;
    [SerializeField] private Image _coverImg;
    [SerializeField] private List<Image> _stars;

    void Awake()
    {
        _coverObject.SetActive(false);
    }

    public void Cover(bool nightVision = false)
    {
        _coverObject.SetActive(true);
        if (!nightVision)
        {
            Debug.Log("U r being covered, u cant see the board");
            _mask.enabled = false;
            _coverUsedItemImg.gameObject.SetActive(false);
            _coverImg.gameObject.SetActive(true);
            _coverImg.DOFade(1, 0.5f).From(0);
        }
        else
        {
            Debug.Log("U r being covered, but u r a night seeker!!!");
            _mask.enabled = true;
            _coverUsedItemImg.gameObject.SetActive(true);
            _coverImg.gameObject.SetActive(false);
        }
        foreach (var star in _stars)
        {
            star.gameObject.SetActive(true);
            star.transform.DORotate(Vector3.right * 30f, 0.2f).SetLoops(-1, LoopType.Yoyo);
        }
    }


    public void Uncover()
    {
        Debug.Log("U r uncovered, u can see the board");
        _coverImg.DOFade(0, 0.5f).From(1).OnComplete(() => _coverObject.SetActive(false));
        foreach (var star in _stars)
        {
            star.gameObject.SetActive(false);
            star.transform.DOKill();
        }
    }
}