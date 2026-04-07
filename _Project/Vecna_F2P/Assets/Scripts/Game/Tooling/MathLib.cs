using UnityEngine;

//Author: Clément DUCROQUET

namespace Com.IsartDigital.F2P.Tooling
{
    internal static class MathLib
    {
        /// <summary>
        /// Gives the Vector2 representing the translation from pVector1 to pVector2.
        /// </summary>
        /// <param name="pVector1"></param>
        /// <param name="pVector2"></param>
        /// <returns></returns>
        public static Vector2 DistanceXY(Vector2 pVector1, Vector2 pVector2)
        {
            return new Vector2(
                pVector2.x - pVector1.x,
                pVector2.y - pVector1.y
                );
        }

        /// <summary>
        /// Gives the float part of a number, between 0 and 1.
        /// </summary>
        /// <param name="pFloat"></param>
        /// <returns></returns>
        public static float Digits(float pFloat)
        {
            return pFloat - Mathf.Floor(pFloat);
        }
    }
}