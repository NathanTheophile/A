using UnityEngine;

//Author: Clément DUCROQUET

namespace Com.IsartDigital.F2P.Tooling.ScriptingData
{
    internal static class InputManagerData
    {
        //Size
        public const uint DEFAULT_TRIGGER_PIXEL_SIZE = 300;
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
        public const uint DEFAULT_DISTANCE_BETWEEN_POINTS = 100;
        public const int DEFAULT_MAX_POINTS_ON_TRAIL = 7;
        public const float DEFAULT_TRAIL_POINT_MAXIMUM_OPACITY = 1f;
        public const float DEFAULT_GRADIENT_DIFF_RATIO_BETWEEN_POINTS = .08f;

        //Joystick Initial Position Ratios
        public const float DEFAULT_JOYSTICK_POSITION_X = .5f; //Middle of the screen
        public const float DEFAULT_JOYSTICK_POSITION_Y = .2f; //Bottom of the screen
    }
}
