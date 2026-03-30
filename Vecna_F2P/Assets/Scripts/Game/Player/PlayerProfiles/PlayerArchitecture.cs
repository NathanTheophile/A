using UnityEngine;

[CreateAssetMenu(fileName = "PlayerArchitecture", menuName = "Scriptable Objects/PlayerArchitecture")]
public class PlayerArchitecture : ScriptableObject
{
    public Vector2 Size;
    public float Speed;
    public float TimeToFullSpeed;
    public float TimeToStop;
    public float RotationSpeed;
}
