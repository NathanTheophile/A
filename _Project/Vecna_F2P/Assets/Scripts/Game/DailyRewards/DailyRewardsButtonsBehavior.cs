using TMPro;
using UnityEngine;

//Author: Clément DUCROQUET

namespace Com.IsartDigital.F2P.DailyRewards
{
    public class DailyRewardsButtonsBehavior : MonoBehaviour
    {
        [SerializeField]
        private Transform lDailyRewardsMenu;

        [SerializeField]
        private string _CoinsTextSuffix;

        public void CloseAction()
        {
            DestroyMenu();
        }

        public void ClaimAction()
        {
            //Collects money, refreshes text displayer.
            RewardsChecker lRewardsChecker = RewardsChecker.GetInstance();
            int lValue = lRewardsChecker.RewardValues[lRewardsChecker.ClampedDay];

            lRewardsChecker.AddCoins(lValue);
            lRewardsChecker.CoinsText.GetChild(0).GetComponent<TextMeshProUGUI>().SetText((lRewardsChecker.Coins + lValue) + _CoinsTextSuffix);

            DestroyMenu();
        }

        private void DestroyMenu()
        {
            Destroy(lDailyRewardsMenu.gameObject);
        }
    }
}