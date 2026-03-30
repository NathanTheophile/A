using PurrNet;
using Unity.VisualScripting;
using UnityEngine;

public class BallFactory : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private Transform spawnPoint;  

    [ContextMenu("Fire")]
    [ServerRpc]
    public void Fire()
    {
        if (!isServer) return;

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

       LogicBall ball = Instantiate(ballPrefab, pos, rot, null).GetComponent<LogicBall>();
        ball.CallTrajectoryAndFire();
    }
}