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

    private readonly Dictionary<int, float> _lastImpactTimeByBallId = new();

    private ImpactState _currentState = ImpactState.Idle;
    private LogicBall _lockedBall;
    private Vector3 _releaseDirection;
    private float _releaseAtTime;

    private Vector3 _knockbackStart;
    private Vector3 _knockbackTarget;
    private float _knockbackStartTime;

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

        _releaseDirection = Vector3.Reflect(incoming.normalized, hitNormal.normalized);

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

        Vector3 lockPosition = _ballLockPoint != null ? _ballLockPoint.position : hitPoint;
        _lockedBall.transform.position = lockPosition;
        _lockedBall.BeginShieldAttach(_player != null ? _player.GetInstanceID() : GetInstanceID(), _ballLockPoint, _lockDuration);

        if (_player != null)
        {
            _knockbackStart = _player.transform.position;

            Vector3 planarRelease = _releaseDirection;
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
            _lockedBall.ReleaseFromShield(_releaseDirection, 1f);

        _lockedBall = null;
        _currentState = ImpactState.Idle;
    }
}
