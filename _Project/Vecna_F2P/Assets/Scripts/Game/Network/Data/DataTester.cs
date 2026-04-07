using UnityEngine;
using UnityEngine.UI;

public class DataTester : MonoBehaviour
{
    [SerializeField] private Text texte;
    private PlayerData playerData;
    public int Gold
    {
        get => playerData.Gold;
        set
        {
            playerData.Gold = value;
            _ = DataManager.Save(playerData);
        }
    }
    public async void Start()
    {
         playerData = await DataManager.Get<PlayerData>();
        RefreshText();
    }
    public void AddCoin() => Gold++;
    public void UnAddCoin() => Gold--;
    public void RefreshText()
    {
        texte.text = playerData.Gold.ToString();
    }
}
