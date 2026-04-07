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

        #region Inspector Properties

        private const uint INSPECTOR_SPACING = 10;

        #endregion

        #region Keyboard Inputs Management

        [Header("Keyboard Inputs")]
        [SerializeField] private KeyCode[] _KeysUp;
        [SerializeField] private KeyCode[] _KeysDown;
        [SerializeField] private KeyCode[] _KeysRight;
        [SerializeField] private KeyCode[] _KeysLeft;

        private Dictionary<Vector2, KeyCode[]> _DirectionalKeys;

        #endregion

        #region Events

        public static event Action<Vector2> OnDirectionalInput;

        #endregion

        #region Joystick UI Components

        //Components
        [Space(INSPECTOR_SPACING)]
        [Header("Joystick Prefabs")]
        [SerializeField, Tooltip("[REQUIRED] The trigger prefab to instantiate at start.")] private Transform _TriggerPrefab;
        [SerializeField, Tooltip("[REQUIRED] The joystick prefab to instantiate on touch.")] private Transform _JoystickCanvasPrefab;
        [SerializeField, Tooltip("[REQUIRED] The trail prefab to instantiate on touch.")] private Transform _TrailPointPrefab;

        //Joystick Module Container
        private Transform _JoystickCanvasTransform;

        //Joystick Component Images
        private Transform _TriggerImageTransform;
        private Transform _JoystickImageTransform;

        //Sprites
        [Space(INSPECTOR_SPACING)]
        [Header("Sprites (Overrides)")]
        [SerializeField, Tooltip("[OPTIONAL] Determines how the trigger will look like. If not specified, the default image will be used instead.")] private Sprite _TriggerSprite;
        [SerializeField, Tooltip("[OPTIONAL] Determines how the joystick will look like. If not specified, the default image will be used instead.")] private Sprite _JoystickSprite;
        [SerializeField, Tooltip("[OPTIONAL] Determines how the trail will look like. If not specified, the default image will be used instead.")] private Sprite _TrailSprite;

        //Trail
        [Space(INSPECTOR_SPACING)]
        [Header("Trail Properties")]
        [SerializeField, Tooltip("The maximum number of points the trail is made of.")] private int _MaxPointsOnTrail = InputManagerData.DEFAULT_MAX_POINTS_ON_TRAIL;
        [SerializeField, Range(0, 1), Tooltip("The trail point maximum opacity. The nearer to the joystick, the more visible.")] private float _TrailPointMaximumOpacity = InputManagerData.DEFAULT_TRAIL_POINT_MAXIMUM_OPACITY;
        [SerializeField, Range(0, 1), Tooltip("The decrease of transparency between each trail point, given as a ratio.")] private float _GradientDiffRatioBetweenPoints = InputManagerData.DEFAULT_GRADIENT_DIFF_RATIO_BETWEEN_POINTS;

        private List<Transform> _TrailPoints = new();
        
        //Settings
        [Space(INSPECTOR_SPACING)]
        [Header("Joystick Parameters")]
        [SerializeField, Tooltip("The trigger size in pixel.")] uint _TriggerPixelSize = InputManagerData.DEFAULT_TRIGGER_PIXEL_SIZE;
        [SerializeField, Tooltip("The joystick size in pixel.")] uint _JoystickPixelSize = InputManagerData.DEFAULT_JOYSTICK_PIXEL_SIZE;
        [SerializeField, Tooltip("The trail point size in pixel.")] uint _TrailPixelSize = InputManagerData.DEFAULT_TRAIL_PIXEL_SIZE;

        [SerializeField, Tooltip("The minimum distance between the trail points. If exceded, will spawn another point.")] uint _DistanceBetweenTrailPoints = InputManagerData.DEFAULT_DISTANCE_BETWEEN_POINTS;

        #endregion

        #region Joystick Behavior

        //Position
        private Vector2 _TouchStartPosition;
        private bool HasMoved;

        //Deadzone
        [SerializeField, Tooltip("The number of pixels that will be ignored after touch before spawning the joystick")] uint _DeadZoneInPixel = InputManagerData.DEFAULT_DEADZONE;

        //Direction strength
        [SerializeField, Tooltip("The strength applied to the joystick. The higher the strength, the faster the player. This is not the player speed, it rather increases it according to the joystick distance to its origin.")] float _FullStrengthDistance = InputManagerData.DEFAULT_FULL_STRENGTH_DISTANCE;

        #endregion

        #region Transition Settings

        [SerializeField, Tooltip("The amount of time the joystick will take to appear on touch.")] private float _JoystickInTransitionTime = InputManagerData.DEFAULT_JOYSTICK_IN_TRANSITION_TIME;
        [SerializeField, Tooltip("The amount of time the joystick will take to disappear on touch release.")] private float _JoystickOutTransitionTime = InputManagerData.DEFAULT_JOYSTICK_OUT_TRANSITION_TIME;

        [SerializeField, Tooltip("The animation curve the joystick will follow during its transition.")] private AnimationCurve _JoystickTransitionAnimationCurve;

        private Coroutine _JoystickTransitionCoroutine;
        private float _TransitionStep = 0;

        #endregion

        #region Window Settings

        //Joystick Position (Screen Ratio)
        [SerializeField, Range(0, 1)] private float _JoystickPositionScreenRatioX;
        [SerializeField, Range(0, 1)] private float _JoystickPositionScreenRatioY;

        //Window Size
        private Vector2 _WindowSize = new Vector2(Screen.width, Screen.height);

        #endregion


        // -- INIT & UPDATE --

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
            _DirectionalKeys = new Dictionary<Vector2, KeyCode[]>()
            {
                { Vector2.up, _KeysUp },
                { Vector2.down, _KeysDown },
                { Vector2.right, _KeysRight },
                { Vector2.left, _KeysLeft }
            };

            //Gets Joystick Components.
            _JoystickCanvasTransform = Instantiate(_JoystickCanvasPrefab, transform);
            _JoystickImageTransform = _JoystickCanvasTransform.GetChild(0).transform;

            CleanJoystickComponents(true); //Hides the joystick on start.
            DisplayTrigger();

            //Applies default properties to joystick components.
            SetComponentProperties(); //Sets all properties such as size and color.
        }

        private void Update()
        {
#if UNITY_EDITOR
            UpdateKeyboardInput();
#endif
            UpdateJoystickInput();
        }


        // -- INPUT MANAGEMENT --

#if UNITY_EDITOR
        /// <summary>
        /// Sets a refreshed input axis Vector2, taking under account specified keyboard inputs.
        /// </summary>
        private void UpdateKeyboardInput()
        {
            Vector2 lCurrentInputData;
            Vector2 lInputAxis = Vector2.zero;
            Vector2 lDirection;

            foreach (KeyValuePair<Vector2, KeyCode[]> lDirectionalKeys in _DirectionalKeys)
            {
                lCurrentInputData = lDirectionalKeys.Key;

                foreach (KeyCode lkey in lDirectionalKeys.Value)
                {
                    if (Input.GetKey(lkey))
                    {
                        lInputAxis += lCurrentInputData;
                        break;
                    }
                }
            }

            lDirection = lInputAxis.normalized;

            if (lDirection != Vector2.zero) OnDirectionalInput?.Invoke(lDirection);
        }
#endif

        /// <summary>
        /// Sets a refreshed input axis Vector2, based on screen touches. Also displays joystick.
        /// </summary>
        private void UpdateJoystickInput()
        {
            Touch lCurrentTouch;
            Vector2 lTouchPosition, lMovementVector, lNormalizedDirection, lDirection;
            float lDistanceToStartPoint, lMovementStrength;

            uint lTouchCount = (uint)Input.touchCount;

            if (lTouchCount == 0)
            {
                //Resets variables & joystick component properties.
                if (HasMoved) HasMoved = false;
                CleanJoystickComponents();
            }
            else //The screen is being touched.
            {
                //Removes the trigger on first touch.
                if (_TriggerImageTransform)
                    Destroy(_TriggerImageTransform.gameObject);

                lCurrentTouch = Input.touches[0]; //Only considers the first touch.
                lTouchPosition = lCurrentTouch.position;

                if (lCurrentTouch.phase == TouchPhase.Began)
                    _TouchStartPosition = lTouchPosition;
                else
                {
                    lDistanceToStartPoint = Vector2.Distance(_TouchStartPosition, lTouchPosition);

                    //Doesn't behave nor show the joystick if there's no finger movement yet (or not strong enough -> deadzone).
                    if (HasMoved)
                    {
                        lMovementVector = MathLib.DistanceXY(_TouchStartPosition, lTouchPosition);
                        lNormalizedDirection = lMovementVector.normalized;

                        //Doesn't consider strength by distance.
                        if (_FullStrengthDistance == 0) //Cannot divide by 0.
                            lDirection = lNormalizedDirection;
                        else
                        {
                            lMovementStrength = Mathf.Clamp((lDistanceToStartPoint - _DeadZoneInPixel) / _FullStrengthDistance, 0, 1);
                            lDirection = lMovementStrength * lNormalizedDirection;
                        }

                        //Joystick Input Signal Emission
                        if (lMovementVector.magnitude > _DeadZoneInPixel)
                            OnDirectionalInput?.Invoke(lDirection);

                        //Draws Joystick.
                        DisplayJoystick(lTouchPosition);
                        DrawJoystickTrail(lTouchPosition, lMovementVector, lDistanceToStartPoint);
                    }
                    else
                        HasMoved = lCurrentTouch.phase != TouchPhase.Stationary && IsTouchStrongEnough(lDistanceToStartPoint);
                }
            }
        }


        // -- INSTANCE CREATION --

        /// <summary>
        /// Create an instance of the specified Transform, sets its size and position.
        /// </summary>
        /// <param name="pInstance"></param>
        /// <param name="pSize"></param>
        /// <param name="pPosition"></param>
        /// <returns>The instanciated object as a Transform.</returns>
        private Transform CreateInstance(Transform pInstance, uint pSize, Vector2 pPosition = default)
        {
            Transform lInstance = Instantiate(pInstance, _JoystickCanvasTransform);
            RectTransform lRectTransform = lInstance.GetComponent<RectTransform>();

            lInstance.position = pPosition;
            lRectTransform.sizeDelta = Vector2.one * pSize;

            return lInstance;
        }


        // -- JOYSTICK PROPERTIES MANAGEMENT --

        /// <summary>
        /// Initializes all needed properties for the joystick.
        /// </summary>
        private void SetComponentProperties()
        {
            //Size
            _JoystickImageTransform.GetComponent<RectTransform>().sizeDelta = Vector2.one * _JoystickPixelSize;

            //Sprite
            if (_JoystickSprite) _JoystickImageTransform.GetComponent<Image>().sprite = _JoystickSprite;
            if (_TriggerSprite) _TriggerImageTransform.GetComponent<Image>().sprite = _TriggerSprite;
        }


        // -- DEADZONE MANAGEMENT --

        /// <summary>
        /// Returns whatever the input movement is greater than the deadzone or not.
        /// </summary>
        /// <returns></returns>
        private bool IsTouchStrongEnough(float pDistance)
        {
            return pDistance > _DeadZoneInPixel;
        }


        // -- VISUAL JOYSTICK MANAGEMENT --

        /// <summary>
        /// Displays an icon to show the screen is touchable.
        /// </summary>
        private void DisplayTrigger()
        {
            Vector2 lJoystickInitialPosition = new Vector2(_JoystickPositionScreenRatioX * _WindowSize.x, _JoystickPositionScreenRatioY * _WindowSize.y);
            _TriggerImageTransform = CreateInstance(_TriggerPrefab, _TriggerPixelSize, lJoystickInitialPosition);
        }

        /// <summary>
        /// Displays the joystick at the touch position. Appears with a custom transition.
        /// </summary>
        /// <param name="pTouchPosition"></param>
        private void DisplayJoystick(Vector2 pTouchPosition)
        {
            _JoystickImageTransform.position = pTouchPosition;

            //Transparent at start, transitioning to full opacity.
            if (_JoystickTransitionCoroutine != null)
                StopCoroutine(_JoystickTransitionCoroutine);

            _JoystickTransitionCoroutine = StartCoroutine(TweenJoystickAlpha(_JoystickInTransitionTime));
        }

        /// <summary>
        /// Draws and animate the joystick trail.
        /// </summary>
        /// <param name="pTouchPosition"></param>
        /// <param name="pDirection"></param>
        /// <param name="pDistance"></param>
        private void DrawJoystickTrail(Vector2 pTouchPosition, Vector2 pDirection, float pDistance)
        {
            #region Trail Point Distances

            float lHalfJoystickSize = _JoystickImageTransform.GetComponent<RectTransform>().sizeDelta.x * .5f;

            float lTotalDistance = Mathf.Max(pDistance - lHalfJoystickSize, 0) / _DistanceBetweenTrailPoints;
            float lTrailPointRatio = MathLib.Digits(lTotalDistance); //Represents the distance ratio between the last point and the next one.

            #endregion

            #region Trail Point Count

            int lUnclampedNumberOfPoints = Mathf.FloorToInt(lTotalDistance);
            int lNumberOfPoints = Mathf.Min(lUnclampedNumberOfPoints, _MaxPointsOnTrail);
            int lBallIndex;

            #endregion


            // -- TRAIL POINT COUNT MANAGEMENT --

            //Removes extra points.
            while (_TrailPoints.Count > lNumberOfPoints)
            {
                lBallIndex = _TrailPoints.Count - 1;
                Destroy(_TrailPoints[lBallIndex].gameObject);
                _TrailPoints.RemoveAt(lBallIndex);
            }

            //Adds missing points.
            while (_TrailPoints.Count < lNumberOfPoints)
                _TrailPoints.Add(CreateInstance(_TrailPointPrefab, _TrailPixelSize));


            // -- ADAPTATIVE TRAIL POINT PROPERTIES --

            SetTrailPointProperties(lUnclampedNumberOfPoints, lNumberOfPoints, lTrailPointRatio, lHalfJoystickSize, pTouchPosition, pDirection);
        }

        /// <summary>
        /// Edits trail points independently on different aspects (transparency, size, position, color and trail stretching).
        /// </summary>
        /// <param name="pUnclampedNumberOfPoints"></param>
        /// <param name="pNumberOfPoints"></param>
        /// <param name="pTrailPointRatio"></param>
        /// <param name="pHalfJoystickSize"></param>
        /// <param name="pTouchPosition"></param>
        /// <param name="pDirection"></param>
        private void SetTrailPointProperties(int pUnclampedNumberOfPoints, int pNumberOfPoints, float pTrailPointRatio, float pHalfJoystickSize, Vector2 pTouchPosition, Vector2 pDirection)
        {
            Transform lTrailPoint;
            int lTrailPointsCount = _TrailPoints.Count;

            #region TRAIL POINT PROPERTIES

            Image lTrailPointImage;
            Color lTrailPointImageColor;
            RectTransform lTrailPointTransform;
            Vector2 lTrailPointSize = Vector2.one * _TrailPixelSize;

            #endregion

            for (int lTrailPointIndex = 0; lTrailPointIndex < lTrailPointsCount; lTrailPointIndex++)
            {
                //Current Trail Point
                lTrailPoint = _TrailPoints[lTrailPointIndex];

                //Gets Properties.
                lTrailPointTransform = lTrailPoint.GetComponent<RectTransform>();
                lTrailPointImage = lTrailPoint.GetComponent<Image>();

                //Sets Properties.
                //Overrides the trail point' sprite if specified in editor.
                if (_TrailSprite) lTrailPointImage.sprite = _TrailSprite;
                lTrailPointImageColor = lTrailPointImage.color;

                //Only if the current point is the nearest to the origin and only once OR only the last point:
                if ((lTrailPointIndex == 0 && pUnclampedNumberOfPoints == _MaxPointsOnTrail) || (lTrailPointIndex == lTrailPointsCount - 1 && pUnclampedNumberOfPoints < _MaxPointsOnTrail))
                    lTrailPointSize = _TrailPixelSize * pTrailPointRatio * Vector2.one;
                else lTrailPointSize = _TrailPixelSize * Vector2.one;

                lTrailPointTransform.sizeDelta = lTrailPointSize;

                //Case number 2: Stretches the trail.
                if (pUnclampedNumberOfPoints >= _MaxPointsOnTrail && pNumberOfPoints == _MaxPointsOnTrail)
                {
                    lTrailPoint.position = pTouchPosition - pDirection + (pDirection - pDirection.normalized * pHalfJoystickSize) * ((float)lTrailPointIndex / pNumberOfPoints);
                    lTrailPointImageColor.a = _TrailPointMaximumOpacity - _GradientDiffRatioBetweenPoints * (lTrailPointsCount - 1 - lTrailPointIndex);
                }

                //Case number 1: Spawns points.
                else
                {
                    //Starts from 0, never equals 1 (= full length).
                    lTrailPoint.position = pTouchPosition - pDirection.normalized * Vector2.one * (_DistanceBetweenTrailPoints * (lTrailPointIndex + 1) + pHalfJoystickSize);
                    lTrailPointImageColor.a = _TrailPointMaximumOpacity - (_GradientDiffRatioBetweenPoints * lTrailPointIndex);
                }

                //Transparency
                lTrailPointImage.color = lTrailPointImageColor;
            }
        }


        // -- JOYSTICK REMOVAL --

        /// <summary>
        /// Removes all visual joystick components.
        /// </summary>
        /// <param name="pIgnoreTransition"></param>
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

            //Destroys Trail
            foreach (Transform pTrailPoint in _TrailPoints)
                Destroy(pTrailPoint.gameObject);

            _TrailPoints.Clear();
        }


        // -- JOYSTICK TRANSITION MANAGEMENT --

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

            //Applies and skips the transition if no duration is specified in editor.
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