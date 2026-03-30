using PurrNet;
using System;
using UnityEngine;

public class LogicBall : NetworkBehaviour
{
    public LogicBall instance;
    [Header("Settings GD")]
    [SerializeField] public float moveSpeed = 15f;
    [SerializeField, Min(0.01f)] private float baseMoveSpeed = 15f;
    [SerializeField, Min(0.01f)] private float maxMoveSpeed = 30f;
    [SerializeField] private int _nBounceMax = 3;
    [SerializeField] private LayerMask _collisionLayer;
    [SerializeField] private int _nBounceTriggerNewTrajectory = 2;

    [Header("Settings Utility")]
    private SyncVar<Vector3[]> _trajectory = new(null);
    private SyncVar<float> _duration = new(0f);

    // Shield lock state (minimal impact on existing LogicBall flow)
    private readonly SyncVar<bool> _isAttachedToShieldSync = new(false);
    private bool _isAttachedToShield = false;
    private Transform _shieldAnchor;
    private Vector3 _attachedFallbackPosition;
    private double _attachTimeoutAt;

    // Trajectory utility 
    private double _startTime = 0f;
    private float _totalPathDistance = 0f;
    private int _currentIndex = 0;
    private bool _isRun = false;
    private bool _isFollowingCustomTrajectory = false;
    private bool _hasRequestedNextTrajectory = false;

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
        Subscib(true);
        _isAttachedToShield = _isAttachedToShieldSync.value;

        if (isServer)
        {
            CallTrajectoryAndFire();
        }
        else
        {
            if (_trajectory.value != null && _trajectory.value.Length > 1)
            {
                float dist = CalculateTotalDistance(_trajectory.value);
                SetupLocalMove(_trajectory.value, dist);
            }
        }
    }
    protected override void OnDespawned(bool asServer)
    {
        base.OnDespawned(asServer);
        Subscib(false);
    }
    private void Subscib(bool pState)
    {
        if (pState)
        {
            _trajectory.onChanged += OnTrajectoryChanged;
            _isAttachedToShieldSync.onChanged += OnShieldAttachChanged;
        }
        else
        {
            _trajectory.onChanged -= OnTrajectoryChanged;
            _isAttachedToShieldSync.onChanged -= OnShieldAttachChanged;
        }
    }
    private void Update()
    {
        if (_isAttachedToShield)
        {
            FollowShieldAnchor();
            return;
        }

        if (_isRun && _trajectory != null && _trajectory.value.Length > 0)
            FlowTrajectory();
    }

    [ContextMenu("CallTrajectoryAndFire")]
    public void CallTrajectoryAndFire()
    {
        _hasRequestedNextTrajectory = true;
        RequestNewTrajectoryRpc(transform.position, transform.forward);
    }

    // Minimal APIs used by shield code without changing existing trajectory internals.
    public void BeginShieldAttach(int playerId, Transform anchorTransform, float duration)
    {
        if (!isServer)
            return;

        _shieldAnchor = anchorTransform;
        _attachedFallbackPosition = anchorTransform != null ? anchorTransform.position : transform.position;
        _attachTimeoutAt = duration > 0f ? GetTime() + duration : double.PositiveInfinity;

        _isAttachedToShield = true;
        _isAttachedToShieldSync.value = true;
        _isRun = false;

        FollowShieldAnchor();
    }

    public void ReleaseFromShield(Vector3 direction, float speedMultiplier)
    {
        if (!isServer)
            return;

        ClearShieldAttachmentState();

        Vector3 releaseDirection = direction.sqrMagnitude < 0.0001f ? transform.forward : direction.normalized;
        float clampedMultiplier = Mathf.Max(1.05f, speedMultiplier);
        float speedReference = Mathf.Max(moveSpeed, Mathf.Max(0.01f, baseMoveSpeed));
        float cappedMaxSpeed = Mathf.Max(speedReference, Mathf.Max(0.01f, maxMoveSpeed));
        moveSpeed = Mathf.Clamp(speedReference * clampedMultiplier, Mathf.Max(0.01f, baseMoveSpeed), cappedMaxSpeed);

        _hasRequestedNextTrajectory = true;
        RequestNewTrajectoryRpc(transform.position, releaseDirection);
    }


    private void FollowShieldAnchor()
    {
        Vector3 anchorPosition = _shieldAnchor != null ? _shieldAnchor.position : _attachedFallbackPosition;
        transform.position = anchorPosition;

        if (_shieldAnchor != null)
            transform.rotation = _shieldAnchor.rotation;

        if (!isServer)
            return;

        if (GetTime() <= _attachTimeoutAt)
            return;

        // Fallback only: state timing authority remains in PlayerShieldImpactController.
        ClearShieldAttachmentState();
        _hasRequestedNextTrajectory = true;
        RequestNewTrajectoryRpc(transform.position, transform.forward);
    }

    private void ClearShieldAttachmentState()
    {
        _isAttachedToShield = false;
        _isAttachedToShieldSync.value = false;
        _shieldAnchor = null;
        _attachTimeoutAt = 0d;
    }

    private void FlowTrajectory()
    {
        float ratio = GetLocalTimeRatio();

        if (ratio >= 1f)
        {
            _isRun = false;
            return;
        }

        Vector3 newPos = GetPositionOnPathConstantly(_trajectory.value, ratio);

        transform.position = new Vector3(newPos.x, transform.position.y, newPos.z);
    }

    private Vector3 GetPositionOnPathConstantly(Vector3[] path, float ratio)
    {
        if (path == null || path.Length < 2) return transform.position;

        float targetDist = ratio * _totalPathDistance;
        float currentDistSum = 0f;

        for (int i = 0; i < path.Length - 1; i++)
        {
            float segmentDist = Vector3.Distance(path[i], path[i + 1]);

            if (currentDistSum + segmentDist >= targetDist)
            {
                if (i > _currentIndex)
                {
                    _currentIndex = i;
                    OnBounceReached();
                }

                float localRatio = (targetDist - currentDistSum) / segmentDist;
                return Vector3.Lerp(path[i], path[i + 1], localRatio);
            }

            currentDistSum += segmentDist;
        }

        return path[path.Length - 1];
    }

    private void OnBounceReached()
    {
        if (_currentIndex + 1 < _trajectory.value.Length)
        {
            Vector3 nextPoint = _trajectory.value[_currentIndex + 1];
            nextPoint.y = transform.position.y;
            transform.LookAt(nextPoint);
        }

        if (!isServer)
            return;

        if (_isFollowingCustomTrajectory)
            return;

        if (_currentIndex >= _nBounceTriggerNewTrajectory && !_hasRequestedNextTrajectory)
        {
            _hasRequestedNextTrajectory = true;
            RequestNewTrajectoryRpc(transform.position, transform.forward);
        }
    }

    [ServerRpc]
    public void RequestNewTrajectoryRpc(Vector3 currentPos, Vector3 currentDir)
    {
        _isFollowingCustomTrajectory = false;
        Vector3[] newPath = TrajectoryRay.GetTrajectoryRay(currentPos, currentDir, _nBounceMax, _collisionLayer);

        if (newPath != null && newPath.Length > 1)
        {
            float dist = CalculateTotalDistance(newPath);

            _isFollowingCustomTrajectory = false;
            _duration.value = dist / moveSpeed;
            _trajectory.value = newPath;

            SetupLocalMove(newPath, dist);
        }
    }

    [ServerRpc]
    public void SetCustomTrajectoryRpc(Vector3[] customPath)
    {
        if (customPath == null || customPath.Length < 2) return;

        float dist = CalculateTotalDistance(customPath);

        _isFollowingCustomTrajectory = true;
        _duration.value = dist / moveSpeed;
        _trajectory.value = customPath;

        SetupLocalMove(customPath, dist);
    }

    public void SetCustomTrajectory(Vector3[] customPath)
    {
        if (isServer)
        {
            SetCustomTrajectoryRpc(customPath);
        }
        else
        {
            SetCustomTrajectoryRpc(customPath);
        }
    }

    private void OnTrajectoryChanged(Vector3[] newPath)
    {
        if (isServer) return;

        if (newPath != null && newPath.Length > 1)
        {
            float dist = CalculateTotalDistance(newPath);
            SetupLocalMove(newPath, dist);
        }
    }

    private void OnShieldAttachChanged(bool attached)
    {
        _isAttachedToShield = attached;

        if (attached)
            _attachedFallbackPosition = transform.position;
        else
            _shieldAnchor = null;
    }

    private void SetupLocalMove(Vector3[] path, float totalDist)
    {
        _totalPathDistance = totalDist;
        _startTime = GetTime();
        _currentIndex = 0;
        _isRun = true;
        _hasRequestedNextTrajectory = false;
    }

    private float GetLocalTimeRatio()
    {
        float duration = _duration.value;
        if (duration <= 0f) return 1f;

        double elapsed = GetTime() - _startTime;
        return Mathf.Clamp01((float)elapsed / duration);
    }

    private float CalculateTotalDistance(Vector3[] path)
    {
        float dist = 0f;
        for (int i = 0; i < path.Length - 1; i++)
            dist += Vector3.Distance(path[i], path[i + 1]);
        return dist;
    }

    private double GetTime() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

    //private void OnDrawGizmos()
    //{
    //    for (int i = 0; i < _trajectory.value.Length - 1; i++)
    //    {
    //        Debug.DrawLine(_trajectory.value[i], _trajectory.value[i + 1]);
    //    }
    //}
}
