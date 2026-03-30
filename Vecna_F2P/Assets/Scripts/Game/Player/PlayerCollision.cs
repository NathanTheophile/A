using PurrNet;
using UnityEngine;

public class PlayerCollision : NetworkBehaviour
{
    [SerializeField] private Player _Player;
    
    void OnTriggerEnter(Collider other)
    {
        LogicBall lBall = other.GetComponent<LogicBall>();
        if (lBall == null) return;

        DestroyEntity(lBall);
    }

    [ObserversRpc]
    private void DestroyEntity(LogicBall lBall)
    {
        Destroy(lBall);
        Destroy(_Player);
    }
}
