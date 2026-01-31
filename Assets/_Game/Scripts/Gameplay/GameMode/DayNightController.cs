using UnityEngine;

public class DayNightController : MonoBehaviour
{
    public void Cover()
    {
        Debug.Log("U r being covered, u cant see the board");
    }

    public void Uncover()
    {
        Debug.Log("U r uncovered, u can see the board");
    }
}