using UnityEngine;
using PurrNet;

public class Player : NetworkBehaviour
{
    [SerializeField] private PlayerArchitecture _PlayerProfile;
    //private float _Speed = 20f;
    //private Vector3 _Direction;

    protected override void OnSpawned()
    {
        base.OnSpawned();

        enabled = isOwner;
        if (GetComponent<PlayerMovement>())
        {
            PlayerMovement lPlayerMove = GetComponent<PlayerMovement>();
            lPlayerMove.UpdateStats(_PlayerProfile.Speed, _PlayerProfile.TimeToFullSpeed, _PlayerProfile.TimeToStop, _PlayerProfile.RotationSpeed);
        }
    }

    
}
