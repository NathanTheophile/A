using UnityEngine;

//Author: Clément DUCROQUET

namespace Com.IsartDigital.F2P.Network.Data
{
    internal class CurrencyData: DataBase
    {
        //Statistics
        public int Coins { get; set; } //Soft Currency
        public int Diamonds { get; set; } //Hard Currency
    }
}