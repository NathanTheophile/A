using UnityEngine;

//Author: Clément DUCROQUET

namespace Com.IsartDigital.F2P.Tooling.ScriptingData
{
    internal static class InputManagerData
    {
        //Size
        public const uint DEFAULT_JOYSTICK_PIXEL_SIZE = 200;
        public const uint DEFAULT_TRAIL_PIXEL_SIZE = 100;

        //Deadzone
        public const uint DEFAULT_DEADZONE = 5;

        //Strength
        public const float DEFAULT_FULL_STRENGTH_DISTANCE = 100f;

        //Transitions
        public const float DEFAULT_JOYSTICK_IN_TRANSITION_TIME = .3f;
        public const float DEFAULT_JOYSTICK_OUT_TRANSITION_TIME = .1f;

        //Trail
        public const uint DEFAULT_MAX_TRAIL_POINTS = 7;
        public const float DEFAULT_GRADIENT_DIFF_RATIO_BETWEEN_POINTS = .08f;
    }
}
