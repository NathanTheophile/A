using PurrNet;
using System;
using UnityEngine;

public class LogicBall : NetworkBehaviour
{
    public LogicBall instance;
    [Header("Settings GD")]
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _maxSpeed;
    [SerializeField] private int _nBounceMax = 3;
    [SerializeField] private LayerMask _collisionLayer;
    [SerializeField] private int _nBounceTriggerNewTrajectory = 2;

    private SyncVar<Vector3[]> _trajectory = new(null);
    private SyncVar<float> _duration = new(0f);
    public float MoveSpeed
    {
        get { return _moveSpeed; }
        set { 
            _moveSpeed = Mathf.Clamp(value,0f, _maxSpeed);
        }
    }

    // Shield lock state
    public bool isAttachedToShield = false;
    private Transform _ShieldAnchor;
    private Vector3 _ShieldAttachStartPosition;
    private double _ShieldAttachStartTime;
    private float _ShieldAttachDuration;

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

        if (isServer) CallTrajectoryAndFire();
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
        if (isAttachedToShield)
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

    public void BeginShieldAttach(int pPlayerId, Transform pAnchorTransform, float pDuration)
    {
        isAttachedToShield = true;
        _isRun = false;
        _ShieldAnchor = pAnchorTransform;
        _ShieldAttachStartPosition = transform.position;
        _ShieldAttachStartTime = GetTime();
        _ShieldAttachDuration = Mathf.Max(0f, pDuration);
    }

    public void ReleaseFromShield(Vector3 pDirection, float pSpeedMultiplier)
    {
        isAttachedToShield = false;
        _ShieldAnchor = null;

        Vector3 lReleaseDirection = pDirection.sqrMagnitude < 0.0001f ? transform.forward : pDirection.normalized;
        MoveSpeed = Mathf.Max(0.01f, MoveSpeed * Mathf.Max(0.01f, pSpeedMultiplier));

        _hasRequestedNextTrajectory = true;
        RequestNewTrajectoryRpc(transform.position, lReleaseDirection);
    }


    private void FollowShieldAnchor()
    {
        if (_ShieldAnchor == null)
            return;

        Vector3 lTargetPos = _ShieldAnchor.position;
        if (_ShieldAttachDuration <= 0f)
        {
            transform.position = lTargetPos;
            return;
        }

        double lElapsed = GetTime() - _ShieldAttachStartTime;
        float lRatio = Mathf.Clamp01((float)lElapsed / _ShieldAttachDuration);
        transform.position = Vector3.Lerp(_ShieldAttachStartPosition, lTargetPos, lRatio);
    }

    private void FlowTrajectory()
    {
        float lRatio = GetLocalTimeRatio();

        if (lRatio >= 1f)
        {
            _isRun = false;
            return;
        }

        Vector3 newPos = GetPositionOnPathConstantly(_trajectory.value, lRatio);

        transform.position = new Vector3(newPos.x, transform.position.y, newPos.z);
    }

    private Vector3 GetPositionOnPathConstantly(Vector3[] lPath, float ratio)
    {
        if (lPath == null || lPath.Length < 2) return transform.position;

        float lTargetDist = ratio * _totalPathDistance;
        float lCurrentDist = 0f;

        for (int i = 0; i < lPath.Length - 1; i++)
        {
            float lSegmentDist = Vector3.Distance(lPath[i], lPath[i + 1]);

            if (lCurrentDist + lSegmentDist >= lTargetDist)
            {
                if (i > _currentIndex)
                {
                    _currentIndex = i;
                    OnBounceReached();
                }

                float lLocalRatio = (lTargetDist - lCurrentDist) / lSegmentDist;
                return Vector3.Lerp(lPath[i], lPath[i + 1], lLocalRatio);
            }

            lCurrentDist += lSegmentDist;
        }

        return lPath[lPath.Length - 1];
    }

    private void OnBounceReached()
    {
        if (_currentIndex + 1 < _trajectory.value.Length)
        {
            Vector3 lNextPoint = _trajectory.value[_currentIndex + 1];
            lNextPoint.y = transform.position.y;
            transform.LookAt(lNextPoint);
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
    public void RequestNewTrajectoryRpc(Vector3 pCurrentPos, Vector3 pCurrentDir)
    {
        _isFollowingCustomTrajectory = false;
        Vector3[] lNewPath = TrajectoryRay.GetTrajectoryRay(pCurrentPos, pCurrentDir, _nBounceMax, _collisionLayer);

        if (lNewPath != null && lNewPath.Length > 1)
        {
            float lDist = CalculateTotalDistance(lNewPath);

            _isFollowingCustomTrajectory = false;
            _duration.value = lDist / MoveSpeed;
            _trajectory.value = lNewPath;

            SetupLocalMove(lNewPath, lDist);
        }
    }

    // PLUS UTILE
    [ServerRpc]
    public void SetCustomTrajectoryRpc(Vector3[] pCustomPath)
    {
        if (pCustomPath == null || pCustomPath.Length < 2) return;

        float dist = CalculateTotalDistance(pCustomPath);

        _isFollowingCustomTrajectory = true;
        _duration.value = dist / MoveSpeed;
        _trajectory.value = pCustomPath;

        SetupLocalMove(pCustomPath, dist);
    }

    public void SetCustomTrajectory(Vector3[] pCustomPath)
    {
        SetCustomTrajectoryRpc(pCustomPath);
    }

    private void OnTrajectoryChanged(Vector3[] lNewPath)
    {
        if (isServer) return;

        if (lNewPath != null && lNewPath.Length > 1)
        {
            float dist = CalculateTotalDistance(lNewPath);
            SetupLocalMove(lNewPath, dist);
        }
    }

    //

    private void SetupLocalMove(Vector3[] lPath, float lTotalDist)
    {
        _totalPathDistance = lTotalDist;
        _startTime = GetTime();
        _currentIndex = 0;
        _isRun = true;
        _hasRequestedNextTrajectory = false;
    }

    private float GetLocalTimeRatio()
    {
        float lDuration = _duration.value;
        if (lDuration <= 0f) return 1f;

        double lElapsed = GetTime() - _startTime;
        return Mathf.Clamp01((float)lElapsed / lDuration);
    }

    private float CalculateTotalDistance(Vector3[] pPath)
    {
        float lDist = 0f;
        for (int i = 0; i < pPath.Length - 1; i++)
            lDist += Vector3.Distance(pPath[i], pPath[i + 1]);
        return lDist;
    }

    private double GetTime() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0d;
}
