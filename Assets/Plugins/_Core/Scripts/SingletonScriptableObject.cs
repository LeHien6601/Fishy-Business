using UnityEngine;

public abstract class SingletonScriptableObject<T> : ScriptableObject where T : ScriptableObject
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                string typeName = typeof(T).Name;
                _instance = Resources.Load<T>(typeName);
                if (_instance == null)
                {
                    Debug.LogError($"{typeName} asset not found in Resources!");
                }
            }
            return _instance;
        }
    }
}