using UnityEngine;

//Author: Mathys MOLES & Clément DUCROQUET

namespace Com.IsartDigital.F2P.Network.Data
{
    internal class RewardsData : DataBase
    {
        //Statistics
        public int LogInTimestamp { get; set; }
        public int LogInStreaks { get; set; }
    }
}