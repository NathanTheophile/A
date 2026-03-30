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

    protected override void OnSpawned()
    {
        base.OnSpawned();

        enabled = isOwner;

    }

    public void Update()
    {
        Debug.DrawRay(transform.position, Vector3.forward);
        _Direction = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        transform.position += _Direction * (Time.deltaTime * _Speed);
        transform.position = new Vector3(transform.position.x, -1f, transform.position.z);
    }

    public void BallNearPlayer()
    {
        
    }
}
