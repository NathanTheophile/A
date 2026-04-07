using PurrNet;
using UnityEditor.UIElements;
using UnityEngine;
[RequireComponent(typeof(Player))]
public class PlayerCloseCollider : NetworkBehaviour
{
    private TagHandle _ballTag;
    private Player _player;
    private void Awake()
    {
        _player = GetComponent<Player>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.TryGetComponent(out LogicBall lBall))
        {
            if (lBall.isAttachedToShield) return;
            _player.HitDamage();
        }
    }
}
