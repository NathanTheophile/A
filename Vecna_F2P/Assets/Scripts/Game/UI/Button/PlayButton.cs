using PurrNet;
using UnityEngine;

public class PlayButton : BaseButton
{
    [SerializeField] private RoomsPlacer placer;
    protected override void OnClick()
    {
        placer.JoinRoom(InstanceHandler.NetworkManager.localPlayer);
        Debug.Log("Press");
    }
}
