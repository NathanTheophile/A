using PurrNet;
using UnityEngine;

public class ExitRoomButton : BaseButton
{
    protected override void OnClick()
    {
        RoomsPlacer.Instance.ExitRoom(InstanceHandler.NetworkManager.localPlayer);
        Debug.Log("Press exit");
    }
}
