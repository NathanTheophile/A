using UnityEngine;
using PurrNet;

public class PlayerCollider : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Player _Player;

    // quand la balle entre dans le premier cercle du
    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        LogicBall ball = other.GetComponent<LogicBall>();

        if (ball == null) return;

        Debug.Log("Je vais déclencher slow");

        SlowMotionTrigger(.5f, ball, .5f);
    }

    // si le joueur se déplace et que la balle ressort du cercle de détection, elle repart vers son forward.
    void OnTriggerExit(Collider other)
    {
        if (!isServer) return;

        LogicBall ball = other.GetComponent<LogicBall>();
        if (ball == null) return;

        SlowMotionTrigger(1f, ball, 2f);
    }

    [ObserversRpc]
    private void SlowMotionTrigger(float pSpeed, LogicBall pBall, float value) { /*if(isServer) pBall.moveSpeed = pBall.moveSpeed * value; Debug.Log("Je déclenche le slow"); Time.timeScale = pSpeed;*/}
}
