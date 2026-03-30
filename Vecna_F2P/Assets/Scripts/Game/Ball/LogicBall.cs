using PurrNet;
using System;
using UnityEngine;

public class LogicBall : NetworkBehaviour
{
    public LogicBall instance;
    [Header("Settings GD")]
    [SerializeField] public float moveSpeed = 15f;
    [SerializeField] private int _nBounceMax = 3;
    [SerializeField] private LayerMask _collisionLayer;
    [SerializeField] private int _nBounceTriggerNewTrajectory = 2;

    [Header("Settings Utility")]
    private SyncVar<Vector3[]> _trajectory = new(null);
    private SyncVar<float> _duration = new(0f);
    private SyncVar<bool> _isAttachedToShieldNet = new(false);
    private SyncVar<Vector3> _shieldAnchorNet = new(Vector3.zero);

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
        if (pState) _trajectory.onChanged += OnTrajectoryChanged;
        else _trajectory.onChanged -= OnTrajectoryChanged;
    }
    private void Update()
    {
        if (_isAttachedToShieldNet.value)
        {
            Vector3 anchor = _shieldAnchorNet.value;
            transform.position = new Vector3(anchor.x, transform.position.y, anchor.z);
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

        Vector3 anchor = anchorTransform != null ? anchorTransform.position : transform.position;
        SetShieldAttachmentStateServer(true, anchor, transform.forward, false);
    }

    public void ReleaseFromShield(Vector3 direction, float speedMultiplier)
    {
        if (!isServer)
            return;

        Vector3 releaseDirection = direction.sqrMagnitude < 0.0001f ? transform.forward : direction.normalized;
        moveSpeed = Mathf.Max(0.01f, moveSpeed * Mathf.Max(0.01f, speedMultiplier));
        SetShieldAttachmentStateServer(false, transform.position, releaseDirection, true);
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

    /// <summary>
    /// Single server entrypoint for attached -> released transitions to avoid server/client path pose conflicts.
    /// </summary>
    private void SetShieldAttachmentStateServer(bool isAttached, Vector3 anchorPosition, Vector3 releaseDirection, bool relaunchTrajectory)
    {
        if (!isServer)
            return;

        StopCurrentTrajectory();
        _shieldAnchorNet.value = anchorPosition;
        _isAttachedToShieldNet.value = isAttached;
        transform.position = new Vector3(anchorPosition.x, transform.position.y, anchorPosition.z);

        if (isAttached || !relaunchTrajectory)
            return;

        _hasRequestedNextTrajectory = true;
        RequestNewTrajectoryRpc(transform.position, releaseDirection);
    }

    private void StopCurrentTrajectory()
    {
        _isRun = false;
        _hasRequestedNextTrajectory = false;
        _isFollowingCustomTrajectory = false;
        _currentIndex = 0;
        _totalPathDistance = 0f;
        _startTime = GetTime();
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
