using Com.IsartDigital.F2P.Managers;
using PurrNet;
using System.Collections;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    public enum MovementLockMode
    {
        Normal,
        RotationOnly
    }

    [SerializeField] public float maxSpeed = 10;
    [SerializeField] public float timeToFull = 0.5f;
    [SerializeField] public float timeToEmpty = 0.5f;
    [SerializeField] public float rotSpeed = 15f;

    private Vector3 _LastInput;
    private float _Inertia = 0;
    private bool _IsHeld = false;
    private bool _Aligned = true;
    private MovementLockMode _movementLockMode = MovementLockMode.Normal;

    private Vector3 _knockbackVelocity = Vector3.zero;
    private Coroutine _unlockCoroutine;

    private void OnEnable()
    {
        InputManager.OnDirectionalInput += OnInput;
    }

    private void Update()
    {
        Move();
        ApplyKnockbackMotion();
        ProgRotate();

        if (_IsHeld) _IsHeld = false;
        transform.position = new Vector3(transform.position.x, 0, transform.position.z);
    }

    private void Move()
    {
        if (_movementLockMode == MovementLockMode.RotationOnly)
            return;

        if (_Inertia != 0)
        {
            transform.position += _LastInput * maxSpeed * _Inertia * Time.deltaTime;
            if (!_IsHeld)
            {
                _Inertia -= Time.deltaTime / timeToEmpty;
                if (_Inertia < 0) _Inertia = 0;
            }
        }
    }

    private void ApplyKnockbackMotion()
    {
        if (_knockbackVelocity.sqrMagnitude <= 0.0001f)
            return;

        transform.position += _knockbackVelocity * Time.deltaTime;
        _knockbackVelocity = Vector3.Lerp(_knockbackVelocity, Vector3.zero, 12f * Time.deltaTime);
    }

    private void ProgRotate()
    {
        if (!_Aligned)
        {
            float lRot = Mathf.Rad2Deg * Mathf.Atan2(-_LastInput.z, _LastInput.x);
            if (lRot < 0) lRot += 360;
            float lCur = transform.eulerAngles.y;
            if (lRot == lCur) _Aligned = true;
            else
            {
                float lPlus = transform.eulerAngles.y + 180;
                if (lPlus > 360) lPlus = -1;
                float lMinus = transform.eulerAngles.y - 180;
                if (lMinus < 0) lMinus = -1;

                if (lPlus != -1)
                {
                    if (lRot < lPlus && lRot > lCur)
                    {
                        lCur += rotSpeed * Time.deltaTime;
                        if (lCur > lRot) lCur = lRot;
                    }
                    else
                    {
                        lCur -= rotSpeed * Time.deltaTime;
                    }
                }
                else if (lMinus != -1)
                {
                    if (lRot < lCur && lRot > lMinus)
                    {
                        lCur -= rotSpeed * Time.deltaTime;
                        if (lCur < lRot) lCur = lRot;
                    }
                    else
                    {
                        lCur += rotSpeed * Time.deltaTime;
                    }
                }

                if (lCur < 0) lCur += 360;
                if (lCur > 360) lCur -= 360;
                transform.eulerAngles = Vector3.up * lCur;
            }
        }
    }

    public void OnInput(Vector2 pInput)
    {
        _LastInput = new Vector3(pInput.x, 0, pInput.y);
        if (_Inertia < 1) _Inertia += Time.deltaTime / timeToFull;
        if (_Inertia > 1) _Inertia = 1;

        _IsHeld = true;
        _Aligned = false;
    }

    public void ApplyKnockbackAndRotationLock(Vector3 knockbackDirection, float knockbackDistance, float knockbackDuration, float lockDuration)
    {
        Vector3 flatDirection = new Vector3(knockbackDirection.x, 0f, knockbackDirection.z).normalized;
        if (flatDirection.sqrMagnitude <= 0.0001f)
            flatDirection = -transform.forward;

        float safeDuration = Mathf.Max(0.05f, knockbackDuration);
        float safeDistance = Mathf.Max(0f, knockbackDistance);
        _knockbackVelocity = flatDirection * (safeDistance / safeDuration);

        SetMovementLockMode(MovementLockMode.RotationOnly);

        if (_unlockCoroutine != null)
            StopCoroutine(_unlockCoroutine);

        _unlockCoroutine = StartCoroutine(UnlockAfterDelay(Mathf.Max(0.05f, lockDuration)));
    }

    private IEnumerator UnlockAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        _knockbackVelocity = Vector3.zero;
        SetMovementLockMode(MovementLockMode.Normal);
        _unlockCoroutine = null;
    }

    public void UpdateStats(float pSpeed, float pFull, float pEmpty, float pRot)
    {
        maxSpeed = pSpeed;
        timeToFull = pFull;
        timeToEmpty = pEmpty;
        rotSpeed = pRot;
    }

    public void SetMovementLockMode(MovementLockMode mode)
    {
        _movementLockMode = mode;
    }

    private void OnDisable()
    {
        InputManager.OnDirectionalInput -= OnInput;
    }
}
