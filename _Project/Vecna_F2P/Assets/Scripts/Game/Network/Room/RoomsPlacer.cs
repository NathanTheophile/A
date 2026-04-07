using PurrNet;
using PurrNet.Modules;
using System.Collections.Generic;
using UnityEngine;

public class RoomsPlacer : NetworkBehaviour
{
    private static RoomsPlacer _instance = null;
    public static RoomsPlacer Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject lGO = new();
                Instance = lGO.AddComponent<RoomsPlacer>();
            }
            return _instance;
        }
        private set
        {
            if (_instance != null)
            {
                Destroy(value.gameObject);
                return;
            }
            _instance = value;
        }
    }

    [Header("Setting")]
    [SerializeField] private bool _canJoinPlayedGame = false;
    [field: SerializeField] public int MaxPlayers { get; private set; } = 2;
    [SerializeField] private string[] _playSceneName;
    [SerializeField] private bool _multiSamePlayerRooms;
    public List<Room> rooms = new();
    private void Awake() => Instance = this;

    [ServerRpc]
    public void JoinRoom(PlayerID pPId)
    {
        if (pPId == InstanceHandler.NetworkManager.localPlayer)
        {
            Debug.LogWarning("server can't join a game");
            return;
        }
        if (_multiSamePlayerRooms && PlayerIsInRoom(pPId)) return;
        Room lRoom = GetGoodRoom();
        if (lRoom.isFull)
        {
            JoinRoom(pPId);
            return;
        }
        lRoom.players.Add(pPId);
        if (!lRoom.isFull) return;
        LancheRoom(lRoom);

    }

    [ServerRpc]
    public void ExitRoom(PlayerID pPId)
    {
        Room lRoom = GetPlayerRoom(pPId);
        if (lRoom == null)
        {
            Debug.Log(pPId.ToString() + " " + "is not in room");
            return;
        }
        lRoom.players.Remove(pPId);
        Debug.Log(pPId.ToString() + " " + "Leav");

        if (!lRoom.isEmpty) return;
        rooms.Remove(lRoom);
        networkManager.sceneModule.UnloadSceneAsync(lRoom.SceneID);
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
            if ((rooms[i].isFull) || (!_canJoinPlayedGame & rooms[i].isPlaying)) continue;
            return rooms[i];
        }
        networkManager.sceneModule.LoadSceneAsync(
                UnityEngine.SceneManagement.SceneUtility
                    .GetBuildIndexByScenePath(_playSceneName[Random.Range(0, _playSceneName.Length)]), new PurrSceneSettings { mode = UnityEngine.SceneManagement.LoadSceneMode.Additive, isPublic = false });
        return CreatRoom();
    }

    private Room CreatRoom()
    {
        Room lRoom = new()
        {
            id = UniqueIndex.NewIndex,
            maxPlayer = MaxPlayers,
            SceneID = networkManager.sceneModule.lastSceneId,
        };
        rooms.Add(lRoom);
        return lRoom;
    }
    //Utility
    private bool PlayerIsInRoom(PlayerID pPID)
    {
        for (int i = 0; i < rooms.Count; i++)
            if (rooms[i].players.Contains(pPID)) return true;
        return false;
    }
    private Room GetPlayerRoom(PlayerID pPID)
    {
        for (int i = 0; i < rooms.Count; i++)
            if (rooms[i].players.Contains(pPID)) return rooms[i];
        return null;
    }
    private void OnApplicationQuit()
    {
        ExitRoom(InstanceHandler.NetworkManager.localPlayer);

        Debug.Log("QUIT");
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
