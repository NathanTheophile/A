using PurrNet;
using UnityEngine;

public class JoinRoomButton : BaseButton
{
    protected override void OnClick()
    {
        RoomsPlacer.Instance.JoinRoom(InstanceHandler.NetworkManager.localPlayer);
        Debug.Log("Press");
    }
}
