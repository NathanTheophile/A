using UnityEngine;
using PurrNet;

public class PlayerExtendedCollider : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Player _Player;

    // quand la balle entre dans le premier cercle du
    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        LogicBall ball = other.GetComponent<LogicBall>();
        if (ball == null) return;
    }

    // si le joueur se déplace et que la balle ressort du cercle de détection, elle repart vers son forward.
    void OnTriggerExit(Collider other)
    {
        if (!isServer) return;

        LogicBall ball = other.GetComponent<LogicBall>();
        if (ball == null) return;
    }
}
