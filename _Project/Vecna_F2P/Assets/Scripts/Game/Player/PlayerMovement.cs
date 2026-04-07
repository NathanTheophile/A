using Com.IsartDigital.F2P.Managers;
using PurrNet;
using UnityEngine;


public class PlayerMovement : NetworkBehaviour
{
    public enum MovementLockMode
    {
        Normal,
        RotationOnly
    }

    [SerializeField] public float maxSpeed = 10;
    [SerializeField] public float timeToMaxSpeed = 0.5f;
    [SerializeField] public float timeToStop = 0.5f;
    [SerializeField] public float rotSpeed = 15f;
    [SerializeField] private bool _RotationStop; //For the rotation stop if you let go of the joystick
    [Header("OnWork")]
    [SerializeField] public float accelerationForce = 50f;
    [SerializeField] public float frictionForce = 50f;


    private Vector3 _Velocity;

    private Vector3 _LastInput;
    private float _Inertia = 0;
    private float _TimeHeld = 0;
    private bool _IsHeld = false;
    private bool _Aligned = true;
    private MovementLockMode _MovementLockMode = MovementLockMode.Normal;
    private Vector3 _ExternalVelocity = Vector3.zero;
    private float _KnockbackStopSpeed = 0f;
    private Camera _Camera;

    public bool lookMouse;
    private void OnEnable()
    {
        InputManager.OnDirectionalInput += OnInput;
        _Camera = Camera.main;  // Don't keep calling Camera.main

    }

    private void Update()
    {
        Move();
        if (lookMouse)
        {
            Vector3 lookAtPos = Input.mousePosition;
            lookAtPos.z = Camera.main.transform.position.y - transform.position.y;
            lookAtPos = Camera.main.ScreenToWorldPoint(lookAtPos);
            transform.forward = lookAtPos - transform.position;
        }
        else
        {
            if (_RotationStop && _IsHeld) ProgRotate();
            else if (!_RotationStop) ProgRotate();
        }




        //Resets every time if not being held.
        if (_IsHeld) _IsHeld = false;
        transform.position = new Vector3(transform.position.x, 0, transform.position.z);
    }

    /// <summary>
    /// Handles movement
    /// </summary>
    private void Move()
    {
        if (_MovementLockMode == MovementLockMode.RotationOnly) return;
        ClampVelocity();
        ApplyVelocity();
        ApplyFriction();
        //if (_ExternalVelocity.sqrMagnitude > 0.0001f)
        //{
        //    transform.position += _ExternalVelocity * Time.deltaTime;
        //    _ExternalVelocity = Vector3.MoveTowards(_ExternalVelocity, Vector3.zero, (_KnockbackStopSpeed) * Time.deltaTime);
        //}


        // if (_Inertia != 0) //Movement if inertia
        // {
        //     //Direction, speed, inertia
        //     transform.position += _LastInput * maxSpeed * _Inertia * Time.deltaTime;
        //     if (!_IsHeld)
        //     {
        //         _Inertia -= Time.deltaTime / timeToEmpty;
        //         if (_Inertia < 0) _Inertia = 0;
        //     }
        // }
    }
    private void ApplyVelocity() => transform.position += _Velocity * Time.deltaTime;
    private void ApplyInputForce() => _Velocity += _LastInput * accelerationForce * Time.deltaTime;
    private void ClampVelocity() => _Velocity = Vector3.ClampMagnitude(_Velocity, maxSpeed);
    private void ApplyFriction()
    {
        Vector3 lvFrictionForce = -_Velocity.normalized * frictionForce * Time.deltaTime;
        lvFrictionForce = Vector3.ClampMagnitude(lvFrictionForce, _Velocity.magnitude);
        _Velocity += lvFrictionForce;
    }
    /// <summary>
    /// Handles rotation if not aligned
    /// </summary>
    private void ProgRotate()
    {
        //Progressive turning concise with quaternions (does not work), using brute force for now.
        /*if (!_Aligned)
        {
            Quaternion lRot = Quaternion.FromToRotation(transform.forward, _LastInput);
            if (lRot == transform.rotation) _Aligned = true;
            else transform.rotation = Quaternion.RotateTowards(transform.rotation, lRot, rotSpeed * Time.deltaTime);
        }*/

        if (!_Aligned) //Brute force method THIS IS NOT GOOD PRACTICE
        {
            float lRot = Mathf.Rad2Deg * Mathf.Atan2(_LastInput.x, _LastInput.z);
            if (lRot < 0) lRot += 360;
            float lCur = transform.eulerAngles.y;
            if (lRot == lCur) _Aligned = true;
            else
            {
                //To go from X to Y. if Y< X -180 && 
                //lRot is Y
                //IF it exceeds the 180 in either of the two, we pick the other option. 181 +180 >360 so we pick 181-180 for rotation.
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
                        //if(lCur < lRot) lCur = lRot;
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
                        //if (lCur > lRot) lCur = lRot;
                    }
                }

                if (lCur < 0) lCur += 360;
                if (lCur > 360) lCur -= 360;
                transform.eulerAngles = Vector3.up * lCur;
            }
        }
    }

    //InputManager calls this or event causes this to get triggered
    public void OnInput(Vector2 pInput)
    {
        //Vector2 pInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));//Temp for testing!!!
        _LastInput = new Vector3(pInput.x, 0, pInput.y);

        if (_Inertia < 1) _Inertia += Time.deltaTime / timeToMaxSpeed;
        if (_Inertia > 1) _Inertia = 1;

        /*float lRot = Mathf.Rad2Deg * Mathf.Atan2(_LastInput.z, _LastInput.x);
        transform.eulerAngles = Vector3.down * lRot;*/
        _IsHeld = true;
        _Aligned = false;
        if (_MovementLockMode == MovementLockMode.RotationOnly) return;
        ApplyInputForce();
    }

    public void UpdateStats(float pSpeed, float pFull, float pEmpty, float pRot)
    {
        maxSpeed = pSpeed;
        timeToMaxSpeed = pFull;
        timeToStop = pEmpty;
        rotSpeed = pRot;
    }

    public void SetMovementLockMode(MovementLockMode mode)
    {
        _MovementLockMode = mode;
    }


    public void ApplyKnockback(Vector3 pDirection, float pDistance, float pDuration)
    {
        if (pDuration <= 0f || pDistance <= 0f || pDirection.sqrMagnitude < 0.0001f)
            return;

        Vector3 lDirection = pDirection.normalized;
        lDirection.y = 0f;

        float speed = pDistance / pDuration;
        _ExternalVelocity = lDirection * speed;
        _KnockbackStopSpeed = speed / pDuration;
    }

    private void OnDisable()
    {
        InputManager.OnDirectionalInput -= OnInput;
    }
}
