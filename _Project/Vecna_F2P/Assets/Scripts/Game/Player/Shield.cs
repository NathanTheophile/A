using System.Collections;
using PurrNet;
using UnityEngine;

public class Shield : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Player _Player;
    [SerializeField] private LayerMask _ShieldLayer;
    [SerializeField] private Transform _BallAnchor;
    public Transform BallAnchor => _BallAnchor;


    [Header("Catch & Recoil")]
    [SerializeField] private float pLockDuration = 0.55f;
    [SerializeField] private float _ReleaseSpeedMultiplier = 1.1f;
    [SerializeField] private float _KnockbackDistance = 1.5f;
    [SerializeField] private float _KnockbackDuration = 0.18f;

    private PlayerMovement _PlayerMovement;
    private bool _isCatching;


    private void Awake()
    {
        if (_Player != null)
            _PlayerMovement = _Player.GetComponent<PlayerMovement>();

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
        if (!isServer)
            return;

        if (_isCatching)
            return;

        LogicBall lBall = other.GetComponent<LogicBall>();
        if (lBall == null)
            return;

        if (!lBall.CanBeCaughtByShield(_BallAnchor))
            return;

        StartCoroutine(CatchAndRecoil(lBall));
    }

    private IEnumerator CatchAndRecoil(LogicBall pBall)
    {
        _isCatching = true;

        if (_PlayerMovement != null)
        {
            Vector3 lRecoilDirection = _Player.transform.position - pBall.transform.position;
            lRecoilDirection.y = 0f;
            _PlayerMovement.ApplyKnockback(lRecoilDirection.normalized, _KnockbackDistance, _KnockbackDuration);
            _PlayerMovement.SetMovementLockMode(PlayerMovement.MovementLockMode.RotationOnly);
        }

        pBall.BeginShieldAttach(_Player != null ? _Player.GetInstanceID() : 0, _BallAnchor, pLockDuration);

        yield return new WaitForSeconds(pLockDuration);

        if (pBall != null)
        {
            Vector3 lReleaseDirection = _Player != null ? _Player.transform.forward : transform.forward;
            lReleaseDirection.y = 0f;
            if (lReleaseDirection.sqrMagnitude < 0.0001f) lReleaseDirection = transform.forward;

            pBall.ReleaseFromShield(lReleaseDirection.normalized, _ReleaseSpeedMultiplier);
        }

        if (_PlayerMovement != null) _PlayerMovement.SetMovementLockMode(PlayerMovement.MovementLockMode.Normal);

        _isCatching = false;
    }
    
}
