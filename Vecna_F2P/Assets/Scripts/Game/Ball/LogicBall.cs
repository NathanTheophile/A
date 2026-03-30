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

    // Shield lock state
    private bool _isAttachedToShield = false;
    private Transform _shieldAnchor;
    private Vector3 _shieldLocalOffset;

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

    public void BeginShieldAttach(int playerId, Transform anchorTransform, float duration)
    {
        _isAttachedToShield = true;
        _isRun = false;
        _shieldAnchor = anchorTransform;

        if (_shieldAnchor != null)
            _shieldLocalOffset = transform.position - _shieldAnchor.position;
        else
            _shieldLocalOffset = Vector3.zero;
    }

    public void ReleaseFromShield(Vector3 direction, float speedMultiplier)
    {
        _isAttachedToShield = false;
        _shieldAnchor = null;

        Vector3 releaseDirection = direction.sqrMagnitude < 0.0001f ? transform.forward : direction.normalized;
        moveSpeed = Mathf.Max(0.01f, moveSpeed * Mathf.Max(0.01f, speedMultiplier));

        _hasRequestedNextTrajectory = true;
        RequestNewTrajectoryRpc(transform.position, releaseDirection);
    }

    private void FollowShieldAnchor()
    {
        if (_shieldAnchor == null)
            return;

        Vector3 targetPos = _shieldAnchor.position + _shieldLocalOffset;
        transform.position = new Vector3(targetPos.x, transform.position.y, targetPos.z);
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
        SetCustomTrajectoryRpc(customPath);
    }

    public void RequestCurveToAnchor(Vector3 anchorPosition, int resolution = 12, float amplitude = 1.2f, bool useSinus = true)
    {
        Vector3[] path = useSinus
            ? BuildSinusoidalPath(transform.position, anchorPosition, Mathf.Max(3, resolution), amplitude)
            : BuildBezierPath(transform.position, anchorPosition, Mathf.Max(3, resolution), amplitude);

        SetCustomTrajectoryRpc(path);
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

    private Vector3[] BuildSinusoidalPath(Vector3 start, Vector3 end, int resolution, float amplitude)
    {
        Vector3[] path = new Vector3[resolution + 1];
        Vector3 toTarget = end - start;
        Vector3 flatDir = Vector3.ProjectOnPlane(toTarget, Vector3.up).normalized;
        if (flatDir.sqrMagnitude < 0.0001f)
            flatDir = transform.forward;

        Vector3 right = Vector3.Cross(Vector3.up, flatDir).normalized;

        for (int i = 0; i <= resolution; i++)
        {
            float t = i / (float)resolution;
            Vector3 point = Vector3.Lerp(start, end, t);
            float sinOffset = Mathf.Sin(t * Mathf.PI) * amplitude;
            point += right * sinOffset;
            point.y = transform.position.y;
            path[i] = point;
        }

        path[0] = start;
        path[path.Length - 1] = new Vector3(end.x, transform.position.y, end.z);
        return path;
    }

    private Vector3[] BuildBezierPath(Vector3 start, Vector3 end, int resolution, float amplitude)
    {
        Vector3[] path = new Vector3[resolution + 1];
        Vector3 toTarget = end - start;
        Vector3 flatDir = Vector3.ProjectOnPlane(toTarget, Vector3.up).normalized;
        if (flatDir.sqrMagnitude < 0.0001f)
            flatDir = transform.forward;

        Vector3 right = Vector3.Cross(Vector3.up, flatDir).normalized;
        Vector3 control = Vector3.Lerp(start, end, 0.5f) + right * amplitude;
        control.y = transform.position.y;

        for (int i = 0; i <= resolution; i++)
        {
            float t = i / (float)resolution;
            Vector3 a = Vector3.Lerp(start, control, t);
            Vector3 b = Vector3.Lerp(control, end, t);
            Vector3 point = Vector3.Lerp(a, b, t);
            point.y = transform.position.y;
            path[i] = point;
        }

        path[0] = start;
        path[path.Length - 1] = new Vector3(end.x, transform.position.y, end.z);
        return path;
    }

    private double GetTime() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
}
