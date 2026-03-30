using Com.IsartDigital.F2P.Managers;
using PurrNet;
using UnityEngine;


public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] public float maxSpeed = 10;
    [SerializeField] public float timeToFull = 0.5f;
    [SerializeField] public float timeToEmpty = 0.5f;
    [SerializeField] public float rotSpeed = 15f;

    private readonly SyncVar<bool> _isMovementLockedSync = new(false);

    private Vector3 _LastInput;
    private float _Inertia = 0;
    private float _TimeHeld = 0;
    private bool _IsHeld = false;
    private bool _Aligned = true;

    private void OnEnable()
    {
        InputManager.OnDirectionalInput += OnInput;
    }

    private void Update()
    {
        Move();
        ProgRotate();
        //Resets every time if not being held.
        if (_IsHeld) _IsHeld = false;
    }

    /// <summary>
    /// Handles movement
    /// </summary>
    private void Move()
    {
        if (_isMovementLockedSync.value)
        {
            _Inertia = 0f;
            return;
        }

        if (_Inertia != 0) //Movement if inertia
        {
            //Direction, speed, inertia
            transform.position += _LastInput * maxSpeed * _Inertia * Time.deltaTime;
            if (!_IsHeld)
            {
                _Inertia -= Time.deltaTime / timeToEmpty;
                if (_Inertia < 0) _Inertia = 0;
            }
        }
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
            float lRot = Mathf.Rad2Deg * Mathf.Atan2(-_LastInput.z, _LastInput.x);
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

        if (_isMovementLockedSync.value)
        {
            _IsHeld = true;
            _Aligned = false;
            return;
        }

        if (_Inertia < 1) _Inertia += Time.deltaTime / timeToFull;
        if (_Inertia > 1) _Inertia = 1;
        /*float lRot = Mathf.Rad2Deg * Mathf.Atan2(_LastInput.z, _LastInput.x);
        transform.eulerAngles = Vector3.down * lRot;*/
        _IsHeld = true;
        _Aligned = false;
    }

    public void UpdateStats(float pSpeed, float pFull, float pEmpty, float pRot)
    {
        maxSpeed = pSpeed;
        timeToFull = pFull;
        timeToEmpty = pEmpty;
        rotSpeed = pRot;
    }

    public void SetMovementLock(bool isLocked)
    {
        if (!isServer)
            return;

        _isMovementLockedSync.value = isLocked;
        if (isLocked)
            _Inertia = 0f;
    }

    private void OnDisable()
    {
        InputManager.OnDirectionalInput -= OnInput;
    }
}
