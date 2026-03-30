using System.Collections.Generic;
using PurrNet;
using UnityEngine;

public class PlayerShieldImpactController : NetworkBehaviour
{
    public enum ImpactState
    {
        Idle,
        ShieldImpactLock,
        Release
    }

    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private Transform _ballLockPoint;

    [Header("State machine")]
    [SerializeField, Min(0.01f)] private float _lockDuration = 0.12f;
    [SerializeField, Min(0.01f)] private float _doubleHitCooldown = 0.08f;

    [Header("Knockback")]
    [SerializeField, Min(0f)] private float _knockbackDistance = 0.35f;
    [SerializeField, Min(0.01f)] private float _knockbackDuration = 0.12f;

    [Header("Release")]
    [SerializeField, Min(0.01f)] private float _releaseSpeed = 1f;
    [SerializeField, Range(0f, 1f)] private float _steerInfluence = 0.35f;
    [SerializeField, Range(0f, 89f)] private float _maxDeflectAngle = 60f;

    [Header("Debug")]
    [SerializeField] private bool _drawDebugVectors = true;
    [SerializeField, Min(0.1f)] private float _debugVectorSize = 1.25f;

    private readonly Dictionary<int, float> _lastImpactTimeByBallId = new();

    private ImpactState _currentState = ImpactState.Idle;
    private LogicBall _lockedBall;
    private Vector3 _incomingDirection;
    private Vector3 _baseReflectionDirection;
    private Vector3 _lastHitNormal;
    private Vector3 _lastReleaseDirection;
    private float _releaseAtTime;

    private Vector3 _knockbackStart;
    private Vector3 _knockbackTarget;
    private float _knockbackStartTime;
    private PlayerMovement _playerMovement;

    public ImpactState CurrentState => _currentState;

    public bool TryHandleImpact(LogicBall ball, Vector3 startPos, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (!isServer || ball == null)
            return false;

        if (!CanProcessImpact(ball))
            return false;

        Vector3 incoming = hitPoint - startPos;
        if (incoming.sqrMagnitude < 0.0001f)
            incoming = ball.transform.forward;

        Vector3 incomingDir = incoming.normalized;
        _incomingDirection = incomingDir;
        _lastHitNormal = hitNormal.sqrMagnitude < 0.0001f ? transform.forward : hitNormal.normalized;
        _baseReflectionDirection = ComputeBaseReflectionDirection(incomingDir, _lastHitNormal);

        BeginLock(ball, hitPoint);
        return true;
    }

    private bool CanProcessImpact(LogicBall ball)
    {
        if (_currentState != ImpactState.Idle)
            return false;

        int id = ball.GetInstanceID();
        if (_lastImpactTimeByBallId.TryGetValue(id, out float lastHitAt))
        {
            if (Time.time - lastHitAt < _doubleHitCooldown)
                return false;
        }

        _lastImpactTimeByBallId[id] = Time.time;
        return true;
    }

    private void BeginLock(LogicBall ball, Vector3 hitPoint)
    {
        _currentState = ImpactState.ShieldImpactLock;
        _lockedBall = ball;
        _releaseAtTime = Time.time + _lockDuration;
        SetMovementLockMode(PlayerMovement.MovementLockMode.RotationOnly);

        Vector3 lockPosition = _ballLockPoint != null ? _ballLockPoint.position : hitPoint;
        _lockedBall.transform.position = lockPosition;
        _lockedBall.BeginShieldAttach(_player != null ? _player.GetInstanceID() : GetInstanceID(), _ballLockPoint, _lockDuration);

        if (_player != null)
        {
            _knockbackStart = _player.transform.position;

            Vector3 planarRelease = _baseReflectionDirection;
            planarRelease.y = 0f;
            if (planarRelease.sqrMagnitude < 0.0001f)
                planarRelease = -_player.transform.forward;

            planarRelease.Normalize();
            _knockbackTarget = _knockbackStart - planarRelease * _knockbackDistance;
            _knockbackTarget.y = _knockbackStart.y;
            _knockbackStartTime = Time.time;
        }
    }

    private void Update()
    {
        if (!isServer)
            return;

        switch (_currentState)
        {
            case ImpactState.Idle:
                break;

            case ImpactState.ShieldImpactLock:
                UpdateShieldImpactLock();
                break;

            case ImpactState.Release:
                ReleaseBall();
                break;
        }
    }

    private void UpdateShieldImpactLock()
    {
        if (_lockedBall == null)
        {
            EndLock();
            return;
        }

        if (_lockedBall != null)
        {
            Vector3 lockPosition = _ballLockPoint != null ? _ballLockPoint.position : _lockedBall.transform.position;
            _lockedBall.transform.position = lockPosition;
        }

        if (_player != null)
        {
            float knockbackRatio = Mathf.Clamp01((Time.time - _knockbackStartTime) / _knockbackDuration);
            _player.transform.position = Vector3.Lerp(_knockbackStart, _knockbackTarget, knockbackRatio);
        }

        if (Time.time >= _releaseAtTime)
            _currentState = ImpactState.Release;
    }

    private void ReleaseBall()
    {
        if (_lockedBall != null)
        {
            _lastReleaseDirection = ComputeReleaseDirection();
            _lockedBall.ReleaseFromShield(_lastReleaseDirection, _releaseSpeed);
        }

        EndLock();
    }

    private void EndLock()
    {
        _lockedBall = null;
        _currentState = ImpactState.Idle;
        SetMovementLockMode(PlayerMovement.MovementLockMode.Normal);
    }

    private void SetMovementLockMode(PlayerMovement.MovementLockMode mode)
    {
        if (_playerMovement == null && _player != null)
            _playerMovement = _player.GetComponent<PlayerMovement>();

        if (_playerMovement != null)
            _playerMovement.SetMovementLockMode(mode);
    }

    private Vector3 ComputeReleaseDirection()
    {
        // Gameplay contract:
        // - Le joueur peut tourner pendant le knockback pour orienter le renvoi.
        // - Le renvoi reste fondé sur l'impact initial (incoming + hit normal), puis seulement influencé.
        Vector3 baseDir = _baseReflectionDirection;
        if (baseDir.sqrMagnitude < 0.0001f)
            baseDir = ComputeBaseReflectionDirection(_incomingDirection, _lastHitNormal);

        baseDir = Vector3.ProjectOnPlane(baseDir, Vector3.up).normalized;
        Vector3 playerForward = Vector3.ProjectOnPlane(
            _player != null ? _player.transform.forward : transform.forward,
            Vector3.up).normalized;

        if (baseDir.sqrMagnitude < 0.0001f) baseDir = transform.forward;
        if (playerForward.sqrMagnitude < 0.0001f) playerForward = baseDir;

        Vector3 steered = Vector3.Slerp(baseDir, playerForward, _steerInfluence).normalized;
        Vector3 finalDir = Vector3.RotateTowards(baseDir, steered, Mathf.Deg2Rad * _maxDeflectAngle, 0f).normalized;
        return finalDir;
    }

    private Vector3 ComputeBaseReflectionDirection(Vector3 incomingDir, Vector3 hitNormal)
    {
        Vector3 safeIncoming = incomingDir.sqrMagnitude < 0.0001f ? transform.forward : incomingDir.normalized;
        Vector3 safeNormal = hitNormal.sqrMagnitude < 0.0001f ? transform.forward : hitNormal.normalized;
        Vector3 baseDir = Vector3.Reflect(safeIncoming, safeNormal);
        if (baseDir.sqrMagnitude < 0.0001f)
            baseDir = -safeIncoming;

        return baseDir.normalized;
    }

    private void OnDrawGizmosSelected()
    {
        if (!_drawDebugVectors)
            return;

        Vector3 debugOrigin = _ballLockPoint != null ? _ballLockPoint.position : transform.position;

        if (_incomingDirection.sqrMagnitude > 0.0001f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(debugOrigin, debugOrigin + _incomingDirection.normalized * _debugVectorSize);
        }

        if (_lastReleaseDirection.sqrMagnitude > 0.0001f)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(debugOrigin, debugOrigin + _lastReleaseDirection.normalized * _debugVectorSize);
        }

        if (_baseReflectionDirection.sqrMagnitude > 0.0001f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(debugOrigin, debugOrigin + _baseReflectionDirection.normalized * _debugVectorSize);
        }
    }
}
