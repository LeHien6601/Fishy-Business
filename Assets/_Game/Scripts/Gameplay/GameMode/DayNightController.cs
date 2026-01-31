using UnityEngine;

public class DayNightController : MonoBehaviour
{
    [SerializeField] private GameObject _coverImg;
    public void Cover()
    {
        Debug.Log("U r being covered, u cant see the board");
        _coverImg.SetActive(true);
    }

    public void Uncover()
    {
        Debug.Log("U r uncovered, u can see the board");
        _coverImg.SetActive(false);
    }
}