using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Author: Clément DUCROQUET

namespace Com.IsartDigital.F2P.DailyRewards
{
    internal class RewardsDisplayer: MonoBehaviour
    {
        #region Singleton

        private static RewardsDisplayer _Instance;
        public static RewardsDisplayer GetInstance()
        {
            if (!_Instance) _Instance = new RewardsDisplayer();
            return _Instance;
        }

        #endregion

        [SerializeField]
        private Transform RewardSectionsContainer;

        [SerializeField]
        private Color _CurrentSectionColor;

        [SerializeField]
        private string _CurrentDayTitle;

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
            => RewardsChecker.OnRewardAvailable += RewardMode;

        private void RewardMode(int pDays, int pHours, int pMinutes, int pSeconds)
        {
            Transform lDailySection = RewardSectionsContainer.GetChild(pDays);

            EditDailySectionColor(lDailySection);
            EditCurrentDayTitle(lDailySection);
        }

        private void EditDailySectionColor(Transform pDailySection)
            => pDailySection.GetComponent<Image>().color = _CurrentSectionColor;

        private void EditCurrentDayTitle(Transform pDailySection)
            => pDailySection.GetChild(0).GetComponent<TextMeshProUGUI>().SetText(_CurrentDayTitle);

        private void OnDestroy()
        {
            #region Singleton

            _Instance = null;

            #endregion
        }
    }
}