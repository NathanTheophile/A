using UnityEngine;

//Author: Clément DUCROQUET

namespace Com.IsartDigital.F2P.Managers
{
    internal struct DirectionalInput
    {
        public Vector2 Direction { get; set; }
        public bool IsTriggered { get; set; }

        public DirectionalInput(Vector2 pDirection)
        {
            Direction = pDirection;
            IsTriggered = default;
        }
    }
}
