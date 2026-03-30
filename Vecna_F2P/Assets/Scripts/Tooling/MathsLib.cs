using UnityEngine;

//Author: Clément DUCROQUET

namespace Com.IsartDigital.F2P.Tooling
{
    internal static class MathsLib
    {
        public static Vector2 DistanceXY(Vector2 pVector1, Vector2 pVector2)
        {
            return new Vector2(
                pVector2.x - pVector1.x,
                pVector2.y - pVector1.y
                );
        }
    }
}