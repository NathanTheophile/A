using UnityEngine;
using PurrNet;

public class Player : NetworkBehaviour
{
    [SerializeField] private PlayerArchitecture _PlayerProfile;
    [SerializeField] private PlayerMovement _playerMovement;

    protected override void OnSpawned()
    {
        base.OnSpawned();

        enabled = isOwner;

        if (_playerMovement == null)
            _playerMovement = GetComponent<PlayerMovement>();

        if (_playerMovement != null)
        {
            _playerMovement.UpdateStats(
                _PlayerProfile.Speed,
                _PlayerProfile.TimeToFullSpeed,
                _PlayerProfile.TimeToStop,
                _PlayerProfile.RotationSpeed);
        }
    }

    public void SetMovementLock(bool isLocked)
    {
        if (_playerMovement == null)
            _playerMovement = GetComponent<PlayerMovement>();

        _playerMovement?.SetMovementLock(isLocked);
    }
}
