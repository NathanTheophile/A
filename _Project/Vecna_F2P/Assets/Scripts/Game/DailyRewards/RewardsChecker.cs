using Com.IsartDigital.F2P.Network.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static UnityEditor.ShaderData;

//Author: Clément DUCROQUET

namespace Com.IsartDigital.F2P.DailyRewards
{
    internal class RewardsChecker: MonoBehaviour
    {
        #region Singleton

        private static RewardsChecker _Instance;
        public static RewardsChecker GetInstance()
        {
            if (!_Instance) _Instance = new RewardsChecker();
            return _Instance;
        }

        #endregion

        #region DATA

        private RewardsData _RewardsData;
        private CurrencyData _CurrencyData;

        #endregion

        #region TIME MANAGEMENT

        private const int UNIX_TIMESTAMP_YEAR = 1970;
        private const int UNIX_TIMESTAMP_MONTH = 1;
        private const int UNIX_TIMESTAMP_DAY = 1;

        private const int MINUTE_TO_SECONDS = 60;
        private const int HOURS_PER_DAY = 24;

        //Reward Availability Time Slot
        [SerializeField] private uint RewardAvailableForHours;

        private int _CurrentLogInTime;
        public int ClampedDay {  get; private set; }

        //Gets the log in time of the last session of the user.
        public int LastLogInTime
        {
            get { return _RewardsData.LogInTimestamp; }
            set
            {
                //Overrides the current value, and saves it.
                _RewardsData.LogInTimestamp = value;
                _ = DataManager.Save(_RewardsData);
            }
        }

        public int LogInStreaks
        {
            get { return _RewardsData.LogInStreaks; }
            set
            {
                //Overrides the current value, and saves it.
                _RewardsData.LogInStreaks = value;
                _ = DataManager.Save(_RewardsData);
            }
        }

        public int Coins
        {
            get { return _CurrencyData.Coins; }
            set
            {
                //Overrides the current value, and saves it.
                _CurrencyData.Coins = value;
                _ = DataManager.Save(_RewardsData);
            }
        }
        #endregion

        #region EVENTS

        public static Action<int, int, int, int> OnRewardAvailable;

        #endregion

        #region UI COMPONENTS

        [SerializeField]
        private Transform _DailyRewardsMenu;

        [SerializeField]
        private Transform _CoinsTextPrefab;

        public Transform CoinsText { get; private set; }

        #endregion

        #region REWARD MANAGEMENT

        [SerializeField]
        private int _NumberOfDays;

        [SerializeField]
        private string _CoinsTextSuffix;

        [SerializeField]
        private List<int> _RewardValues;

        public List<int> RewardValues { get { return _RewardValues; } }

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

        private async void Start()
        {
            //Loads data from SQL stores.
            _RewardsData = await DataManager.Get<RewardsData>();
            _CurrencyData = await DataManager.Get<CurrencyData>();

            InstantiateCoinsText();
            CoinsText.GetChild(0).GetComponent<TextMeshProUGUI>().SetText(Coins.ToString() + _CoinsTextSuffix);

            //Gets the current time when the user logs in.
            _CurrentLogInTime = GetUnixTimestamp();

            //Checks if the user has logged in during the correct time slot.
            CheckForReward();
        }

        /// <summary>
        /// Creates a coins text displayer.
        /// </summary>
        private void InstantiateCoinsText()
            => CoinsText = Instantiate(_CoinsTextPrefab, transform);

        /// <summary>
        /// Adds a certain amoint of coins.
        /// </summary>
        /// <param name="pValue"></param>
        public void AddCoins(int pValue)
            => Coins += pValue;

        /// <summary>
        /// Checks if the user logged in during the matching time slot.
        /// </summary>
        private void CheckForReward()
        {
            bool lRewardAvailable;
            int lHours, lMinutes, lSeconds;

            ClampedDay = (LogInStreaks % _NumberOfDays);

            int lElapsedTime = GetElapsedTime(_CurrentLogInTime, LastLogInTime);
            float lHalfRewardHours = RewardAvailableForHours * .5f;

            float lTimeSlotMin = HOURS_PER_DAY - lHalfRewardHours;
            float lTimeSlotMax = HOURS_PER_DAY + lHalfRewardHours;

            (lHours, lMinutes, lSeconds) = FromTimestampToDate(lElapsedTime);

            lRewardAvailable = lHours > lTimeSlotMin && lHours < lTimeSlotMax;

            Debug.Log("Edit the line below before any push.");
            if (true) // (lRewardAvailable)
            {
                StartCoroutine(TriggerMenu(lHours, lMinutes, lSeconds));

                LastLogInTime = _CurrentLogInTime;
                ++LogInStreaks;
            }
        }

        /// <summary>
        /// Instantiates the menu and invokes an event with the collected data.
        /// </summary>
        /// <param name="pHours"></param>
        /// <param name="pMinutes"></param>
        /// <param name="pSeconds"></param>
        /// <returns></returns>
        private IEnumerator TriggerMenu(int pHours, int pMinutes, int pSeconds)
        {
            Instantiate(_DailyRewardsMenu, transform);
            yield return null; //Event is detected thanks to the coroutine yield here.
            OnRewardAvailable?.Invoke(ClampedDay, pHours, pMinutes, pSeconds);
        }

        /// <summary>
        /// Converts a timestamp to hours, minutes and seconds.
        /// </summary>
        /// <param name="pTimestamp"></param>
        /// <returns>Hours, minutes and seconds independently.</returns>
        private (int, int, int) FromTimestampToDate(int pTimestamp)
        {
            int lHours, lMinutes, lSeconds;
            int lHoursInSecond, lMinutesInSecond;

            int lPowHours = MINUTE_TO_SECONDS * MINUTE_TO_SECONDS;


            lHours = Mathf.FloorToInt(pTimestamp / lPowHours);
            lHoursInSecond = lHours * lPowHours;

            lMinutes = Mathf.FloorToInt((pTimestamp - lHoursInSecond) / MINUTE_TO_SECONDS);
            lMinutesInSecond = lMinutes * MINUTE_TO_SECONDS;

            lSeconds = Mathf.FloorToInt(pTimestamp - lHoursInSecond - lMinutesInSecond);


            return (lHours, lMinutes, lSeconds);
        }

        /// <summary>
        /// Compares two time points and give the number of seconds elapsed between the two of them.
        /// </summary>
        /// <param name="lUtcNow">The most recent time point.</param>
        /// <param name="lUtcLast">The oldest time point.</param>
        /// <returns>The elapsed time between two points.</returns>
        private int GetElapsedTime(int lUtcNow, int lUtcLast)
        {
            return lUtcNow - lUtcLast;
        }

        /// <summary>
        /// Calculates and returns the number of seconds elapsed since January the 1st 1970.
        /// </summary>
        /// <returns>The Unix timestamp in seconds.</returns>
        private int GetUnixTimestamp()
        {
            DateTime lStartPoint = new DateTime(
                UNIX_TIMESTAMP_YEAR,
                UNIX_TIMESTAMP_MONTH,
                UNIX_TIMESTAMP_DAY);
            DateTime lUtcNow = DateTime.UtcNow;

            return (int)(lUtcNow - lStartPoint).TotalSeconds;
        }

        private void OnDestroy()
        {
            #region Singleton

            _Instance = null;

            #endregion
        }
    }
}
