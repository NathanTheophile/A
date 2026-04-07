using UnityEngine;

public class DontDestroyLoadObject : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(this);
    }
}
