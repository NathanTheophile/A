using System.Collections;
using PurrNet;
using UnityEngine;

public class ShieldCollider : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Player _Player;
    [SerializeField] private LayerMask _ShieldLayer;
    [SerializeField] private Transform _BallAnchor;

    [Header("Catch & Recoil")]
    [SerializeField] private float _lockDuration = 0.55f;
    [SerializeField] private float _releaseSpeedMultiplier = 1.1f;
    [SerializeField] private float _knockbackDistance = 1.5f;
    [SerializeField] private float _knockbackDuration = 0.18f;

    private PlayerMovement _playerMovement;
    private bool _isCatching;

    public Transform BallAnchor => _BallAnchor;

    private void Awake()
    {
        if (_Player != null)
            _playerMovement = _Player.GetComponent<PlayerMovement>();

        if (_BallAnchor == null)
            _BallAnchor = transform;
    }

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
        if (_isCatching)
            return;

        LogicBall ball = other.GetComponent<LogicBall>();
        if (ball == null)
            return;

        StartCoroutine(CatchAndRecoil(ball));
    }

    private IEnumerator CatchAndRecoil(LogicBall ball)
    {
        _isCatching = true;

        if (_playerMovement != null)
        {
            Vector3 recoilDirection = (_Player.transform.position - ball.transform.position);
            recoilDirection.y = 0f;
            _playerMovement.ApplyKnockback(recoilDirection.normalized, _knockbackDistance, _knockbackDuration);
            _playerMovement.SetMovementLockMode(PlayerMovement.MovementLockMode.RotationOnly);
        }

        ball.BeginShieldAttach(_Player != null ? _Player.GetInstanceID() : 0, _BallAnchor, _lockDuration);

        yield return new WaitForSeconds(_lockDuration);

        if (ball != null)
        {
            Vector3 releaseDirection = _Player != null ? _Player.transform.forward : transform.forward;
            releaseDirection.y = 0f;
            if (releaseDirection.sqrMagnitude < 0.0001f)
                releaseDirection = transform.forward;

            ball.ReleaseFromShield(releaseDirection.normalized, _releaseSpeedMultiplier);
        }

        if (_playerMovement != null)
            _playerMovement.SetMovementLockMode(PlayerMovement.MovementLockMode.Normal);

        _isCatching = false;
    }
}
