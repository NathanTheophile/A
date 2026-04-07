using PurrNet;
using UnityEngine;

public class BallFactory : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private Transform spawnPoint;

    [ContextMenu("Fire")]
    [ServerRpc]
    public void FireServer()
    {
        if (!isServer) return;
        FireObservers();
    }
    [ObserversRpc]
    public void FireObservers()
    {
        Vector3 lPos = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion lRot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        LogicBall lBall = Instantiate(ballPrefab, lPos, lRot, null).GetComponent<LogicBall>();
        networkManager.Spawn(lBall.gameObject);
    }

}