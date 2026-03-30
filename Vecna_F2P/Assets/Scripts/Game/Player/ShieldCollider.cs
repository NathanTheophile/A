using PurrNet;
using System.Collections;
using UnityEngine;

public class ShieldCollider : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Player _Player;
    [SerializeField] private Transform _BallAnchor;

    [Header("Catch & Recoil")]
    [SerializeField] private float _CatchDuration = 0.45f;
    [SerializeField] private float _KnockbackDistance = 1.75f;
    [SerializeField] private float _KnockbackDuration = 0.2f;
    [SerializeField] private float _ReleaseSpeedMultiplier = 1.1f;
    [SerializeField] private float _CatchCooldown = 0.1f;
    [SerializeField] private LayerMask _ShieldLayer;

    private bool _isCatching;

    private void OnValidate()
    {
        Collider shieldCollider = GetComponent<Collider>();
        if (shieldCollider != null && (_ShieldLayer.value & (1 << shieldCollider.gameObject.layer)) == 0)
        {
            Debug.LogWarning($"Le layer du shield '{LayerMask.LayerToName(shieldCollider.gameObject.layer)}' n'est pas dans _ShieldLayer.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_Player == null || !_Player.isOwner || _isCatching)
            return;

        LogicBall ball = other.GetComponent<LogicBall>();
        if (ball == null)
            return;

        PlayerMovement movement = _Player.GetComponent<PlayerMovement>();
        if (movement == null)
            return;

        StartCoroutine(CatchAndRecoil(ball, movement));
    }

    private IEnumerator CatchAndRecoil(LogicBall ball, PlayerMovement movement)
    {
        _isCatching = true;

        Vector3 recoilDir = (_Player.transform.position - ball.transform.position).normalized;
        movement.ApplyKnockbackAndRotationLock(recoilDir, _KnockbackDistance, _KnockbackDuration, _CatchDuration);

        Transform anchor = _BallAnchor != null ? _BallAnchor : transform;
        ball.BeginShieldAttach(_Player.GetInstanceID(), anchor, _CatchDuration);

        yield return new WaitForSeconds(_CatchDuration);

        Vector3 releaseDirection = _Player.transform.forward;
        ball.ReleaseFromShield(releaseDirection, _ReleaseSpeedMultiplier);

        yield return new WaitForSeconds(_CatchCooldown);
        _isCatching = false;
    }
}
