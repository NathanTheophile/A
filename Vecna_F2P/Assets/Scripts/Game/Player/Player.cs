using UnityEngine;
using PurrNet;

public class Player : NetworkBehaviour
{
    [SerializeField] private PlayerArchitecture _PlayerProfile;
    [SerializeField] private Collider playerCollider;
    public float _Speed = 20f;
    private Vector3 _Direction;
    public float _RotationSpeed = 4f;
    public int _RemainingLife = 3;
    private bool _IsMovementLocked = false;

    protected override void OnSpawned()
    {
        base.OnSpawned();

        enabled = isOwner;

    }

    public void Update()
    {
        Debug.DrawRay(transform.position, Vector3.forward);
        _Direction = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

        if (_Direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_Direction.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _RotationSpeed * Time.deltaTime);
        }

        if (!_IsMovementLocked)
        {
            transform.position += _Direction * (Time.deltaTime * _Speed);
            transform.position = new Vector3(transform.position.x, -1f, transform.position.z);
        }
    }

    public void BallNearPlayer()
    {
        
    }

    public void SetMovementLock(bool isLocked)
    {
        _IsMovementLocked = isLocked;
    }
}
