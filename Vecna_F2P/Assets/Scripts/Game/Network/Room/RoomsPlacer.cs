using PurrNet;
using PurrNet.Modules;
using System.Collections.Generic;
using UnityEngine;

public class RoomsPlacer : NetworkBehaviour
{
    [Header("Setting")]
    [SerializeField] private bool _canJoinPlayedGame = false;
    [SerializeField] private int _maxPlayers = 2;
    [SerializeField] private string[] _playSceneName;

    public List<Room> rooms = new();

    [ServerRpc]
    public void JoinRoom(PlayerID pPId)
    {
        Room room = GetGoodRoom();
        if (room.isFull)
        {
            JoinRoom(pPId);
            return;
        }
        room.players.Add(pPId);
        if (room.isFull) LancheRoom(room);
    }
    private void LancheRoom(Room pRoom)
    {
        pRoom.isPlaying = true;
        ScenePlayersModule scenePlayers = networkManager.GetModule<ScenePlayersModule>(true);

        foreach (PlayerID lPId in pRoom.players)
            scenePlayers.MovePlayerToSingleScene(lPId, pRoom.SceneID);
    }
    private Room GetGoodRoom()
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            if ((rooms[i].isFull) | (!_canJoinPlayedGame & rooms[i].isPlaying)) continue;
            return rooms[i];
        }
        networkManager.sceneModule.LoadSceneAsync(
                UnityEngine.SceneManagement.SceneUtility
                    .GetBuildIndexByScenePath(_playSceneName[Random.Range(0, _playSceneName.Length)]), new PurrSceneSettings { mode = UnityEngine.SceneManagement.LoadSceneMode.Additive,isPublic=false });
        return CreatRoom();
    }

    private Room CreatRoom()
    {
        Room lRoom = new()
        {
            id = UniqueIndex.NewIndex,
            maxPlayer = _maxPlayers,
            SceneID = networkManager.sceneModule.lastSceneId,
        };
        rooms.Add(lRoom);
        return lRoom;
    }
}

public static class UniqueIndex
{
    private static int _index;
    public static int NewIndex
    {
        get
        {
            _index++;
            return _index;
        }
    }
}
