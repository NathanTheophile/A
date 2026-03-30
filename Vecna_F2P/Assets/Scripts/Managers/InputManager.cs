using Com.IsartDigital.F2P.Tooling;
using Com.IsartDigital.F2P.Tooling.ScriptingData;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//Author: Clément DUCROQUET

namespace Com.IsartDigital.F2P.Managers
{
    internal class InputManager : MonoBehaviour
    {
        #region Singleton

        private static InputManager _Instance;
        public static InputManager GetInstance()
        {
            if (!_Instance) _Instance = new InputManager();
            return _Instance;
        }

        #endregion

        #region Keyboard Inputs Management

        [SerializeField] private KeyCode[] _KeysUp;
        [SerializeField] private KeyCode[] _KeysDown;
        [SerializeField] private KeyCode[] _KeysRight;
        [SerializeField] private KeyCode[] _KeysLeft;

        private DirectionalInput _DirectionUp = new(Vector3.up);
        private DirectionalInput _DirectionDown = new(Vector3.down);
        private DirectionalInput _DirectionRight = new(Vector3.right);
        private DirectionalInput _DirectionLeft = new(Vector3.left);

        private Dictionary<DirectionalInput, KeyCode[]> _DirectionalKeys;

        #endregion

        #region Events

        public static event Action<Vector2> OnDirectionalInput;

        #endregion

        #region Joystick UI Components

        //Components
        [SerializeField] private Transform _JoystickCanvasPrefab;

        private Transform _JoystickCanvasTransform;
        private Transform _JoystickImageTransform;

        //Sprites
        [SerializeField] private Sprite _JoystickSprite;
        [SerializeField] private Sprite _TrailSprite;

        //Settings
        [SerializeField] uint _JoystickPixelSize = InputManagerData.DEFAULT_JOYSTICK_PIXEL_SIZE;
        [SerializeField] uint _TrailPixelSize = InputManagerData.DEFAULT_TRAIL_PIXEL_SIZE;

        #endregion

        #region Joystick Behavior

        //Position
        private Vector2 _TouchStartPosition;
        private bool HasMoved;

        //Deadzone
        [SerializeField] uint _DeadZoneInPixel = InputManagerData.DEFAULT_DEADZONE;

        //Direction strength
        [SerializeField] float _FullStrengthDistance = InputManagerData.DEFAULT_FULL_STRENGTH_DISTANCE;

        #endregion

        #region Transition Settings

        [SerializeField] private float _JoystickInTransitionTime = InputManagerData.DEFAULT_JOYSTICK_IN_TRANSITION_TIME;
        [SerializeField] private float _JoystickOutTransitionTime = InputManagerData.DEFAULT_JOYSTICK_OUT_TRANSITION_TIME;

        [SerializeField] private AnimationCurve _JoystickTransitionAnimationCurve;

        private Coroutine _JoystickTransitionCoroutine;
        private float _TransitionStep = 0;

        #endregion

        private void Awake()
        {
            #region Singleton

            if (_Instance)
            {
                Destroy(this);
                return;
            }
            
            _Instance = this;

            #endregion
        }

        private void Start()
        {
            //Keyboard Inputs Init
            _DirectionalKeys = new Dictionary<DirectionalInput, KeyCode[]>()
            {
                { _DirectionUp, _KeysUp },
                { _DirectionDown, _KeysDown },
                { _DirectionRight, _KeysRight },
                { _DirectionLeft, _KeysLeft }
            };

            //Gets Joystick Components.
            _JoystickCanvasTransform = Instantiate(_JoystickCanvasPrefab);
            _JoystickImageTransform = _JoystickCanvasTransform.GetChild(0).transform;

            CleanJoystickComponents(true); //Hides the joystick on start.
            SetComponentProperties(); //Sets all properties such as size, color, etc.
        }

        private void Update()
        {
#if UNITY_EDITOR
            UpdateKeyboardInput();
#endif
            UpdateJoystickInput();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Sets a refreshed input axis Vector2, taking under account specified keyboard inputs.
        /// </summary>
        private void UpdateKeyboardInput()
        {
            DirectionalInput lCurrentInputData;
            Vector2 lInputAxis = Vector2.zero;

            foreach (KeyValuePair<DirectionalInput, KeyCode[]> lDirectionalKeys in _DirectionalKeys)
            {
                lCurrentInputData = lDirectionalKeys.Key;

                foreach (KeyCode lkey in lDirectionalKeys.Value)
                {
                    if (Input.GetKey(lkey) && !lCurrentInputData.IsTriggered)
                    {
                        lCurrentInputData.IsTriggered = true;
                        lInputAxis += lCurrentInputData.Direction;
                    }
                }
            }

            if (lInputAxis != Vector2.zero) OnDirectionalInput?.Invoke(lInputAxis);
        }
#endif

        /// <summary>
        /// Sets a refreshed input axis Vector2, based on screen touches. Also displays joystick on screen.
        /// </summary>
        private void UpdateJoystickInput()
        {
            Touch lCurrentTouch;
            Vector2 lTouchPosition, lNormalizedDirection, lDirection;
            float lDistanceToStartingPosition;

            uint lTouchCount = (uint)Input.touchCount;

            if (lTouchCount == 0)
            {
                if (HasMoved) HasMoved = false;
                CleanJoystickComponents();
            }
            else
            {
                lCurrentTouch = Input.touches[0]; //Only considers the first touch.
                lTouchPosition = lCurrentTouch.position;

                if (lCurrentTouch.phase == TouchPhase.Began)
                    _TouchStartPosition = lTouchPosition;
                else
                {
                    lDistanceToStartingPosition = Vector2.Distance(_TouchStartPosition, lTouchPosition);

                    //Doesn't show the joystick if there's no finger movement yet (or not strong enough).
                    if (HasMoved)
                    {
                        //Draws Joystick
                        DisplayJoystick(lTouchPosition);
                        DrawJoystickTrail(_TouchStartPosition, lTouchPosition);

                        lNormalizedDirection = MathsLib.DistanceXY(_TouchStartPosition, lTouchPosition).normalized;

                        if (_FullStrengthDistance == 0) //Cannot divide by 0.
                            lDirection = lNormalizedDirection;
                        else
                            //Multiplies float / float * Vector2 for better optimisation.
                            lDirection = Mathf.Min(lDistanceToStartingPosition / _FullStrengthDistance, 1) * lNormalizedDirection;

                        //Joystick Input Signal Emission
                        if (lDistanceToStartingPosition > _DeadZoneInPixel)
                            OnDirectionalInput?.Invoke(lDirection);
                    }
                    else
                        HasMoved = lCurrentTouch.phase != TouchPhase.Stationary && IsTouchStrongEnough(lDistanceToStartingPosition);
                }
            }
        }

        private void DisplayJoystick(Vector2 pTouchPosition)
        {
            //Shows the joystick at the touch position.
            _JoystickImageTransform.position = pTouchPosition;

            //Transparent at start, transitioning to full opacity.
            if (_JoystickTransitionCoroutine != null)
                StopCoroutine(_JoystickTransitionCoroutine);

            _JoystickTransitionCoroutine = StartCoroutine(TweenJoystickAlpha(_JoystickInTransitionTime));
        }

        private void DrawJoystickTrail(Vector2 pStartTouchPosition, Vector2 pCurrentTouchPosition)
        {

        }

        private void CleanJoystickComponents(bool pIgnoreTransition = false)
        {
            Image lJoystickImage;
            Color lJoystickColor;

            //Hides the joystick.
            if (pIgnoreTransition)
            {
                lJoystickImage = _JoystickImageTransform.GetComponent<Image>();
                lJoystickColor = lJoystickImage.color;

                lJoystickColor.a = 0;
                lJoystickImage.color = lJoystickColor;
            }
            else
            {
                if (_JoystickTransitionCoroutine != null)
                    StopCoroutine(_JoystickTransitionCoroutine);

                _JoystickTransitionCoroutine = StartCoroutine(TweenJoystickAlpha(_JoystickOutTransitionTime, true));
            }
        }

        /// <summary>
        /// Returns whatever the input movement is greater than the deadzone or not.
        /// </summary>
        /// <returns></returns>
        private bool IsTouchStrongEnough(float pDistance)
        {
            return pDistance > _DeadZoneInPixel;
        }

        /// <summary>
        /// 
        /// </summary>
        private void SetComponentProperties()
        {
            //Size
            _JoystickImageTransform.GetComponent<RectTransform>().sizeDelta = Vector2.one * _JoystickPixelSize;

            //Sprites
            if (_JoystickSprite) _JoystickImageTransform.GetComponent<Image>().sprite = _JoystickSprite;
        }

        /// <summary>
        /// Executes a transition from an alpha state to its opposite (full transparency to full opacity, or the reverse).
        /// </summary>
        /// <param name="pTransitionTime"></param>
        /// <param name="pOpaqueToTransparent"></param>
        /// <returns></returns>
        private IEnumerator TweenJoystickAlpha(float pTransitionTime, bool pOpaqueToTransparent = false)
        {
            float lElapsedTime = _TransitionStep * pTransitionTime;

            Image lJoystickImage = _JoystickImageTransform.GetComponent<Image>();
            Color lJoystickColor = lJoystickImage.color;

            if (pTransitionTime == 0)
            {
                lJoystickColor.a = 1;
                lJoystickImage.color = lJoystickColor;
                yield return null;
            }

            while (pOpaqueToTransparent ? lElapsedTime > 0 : lElapsedTime < pTransitionTime)
            {
                lElapsedTime += Time.deltaTime * (pOpaqueToTransparent ? -1 : 1);
                //It multiplies the elapsed time to fit the same step with a different transition time.
                _TransitionStep = Mathf.Clamp(lElapsedTime / pTransitionTime, 0, 1);

                lJoystickColor.a = Mathf.Lerp(0, 1, _JoystickTransitionAnimationCurve.Evaluate(_TransitionStep));
                lJoystickImage.color = lJoystickColor;

                yield return null;
            }
        }

        private void OnDestroy()
        {
            #region Singleton

            _Instance = null;

            #endregion
        }
    }
}