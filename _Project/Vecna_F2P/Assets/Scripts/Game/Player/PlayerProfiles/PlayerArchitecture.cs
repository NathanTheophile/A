using System;
using UnityEngine;

namespace Com.IsartDigital.F2P.Player.PlayerProfiles
{
    [CreateAssetMenu(fileName = "PlayerArchitecture", menuName = "Scriptable Objects/PlayerArchitecture")]
    public class PlayerAchitecture : ScriptableObject
    {
        //Character Properties
        [SerializeField, Tooltip("The maximum speed per second the player can reach while moving.")]
        private float _MovingSpeed;
        public float MovingSpeed { get { return _MovingSpeed; } }

        [SerializeField, Tooltip("The delay needed so the player reaches its maximum speed.")]
        private float _TimeToFullSpeed;
        public float TimeToFullSpeed { get { return _TimeToFullSpeed; } }

        [SerializeField, Tooltip("The delay needed so the player fully stops.")]
        private float _TimeToDecelerate;
        public float Deceleration { get { return _TimeToDecelerate; } }

        [SerializeField, Tooltip("The speed (in degrees) the player will rotate per second.")]
        private float _RotationSpeed;
        public float RotationSpeed { get { return _RotationSpeed; } }

        //Equipment Properties
        [SerializeField, Tooltip("The size of the shield object.")]
        private Vector2 _ShieldSize;
        public Vector2 ShieldSize { get { return _ShieldSize; } }

        [SerializeField, Tooltip("The shape of the shield object.")]
        private CharacterShield _ShieldShape;
        public CharacterShield ShieldShape { get { return _ShieldShape; } }

        //Power Settings
        [SerializeField, Tooltip("The unique power of the current character.")] private CharacterPower _Power;
        public CharacterPower Power { get { return _Power; } }
    }

    public enum CharacterShield { }

    public enum CharacterPower { }
}
