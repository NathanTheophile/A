using PurrNet;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    private static GameManager _instance = null;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject lGO = new();
                Instance = lGO.AddComponent<GameManager>();
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

    [Header("Game Setting")]
    [SerializeField] private int _nRounds = 2;
    [Header("Team")]
    [SerializeField] private Transform _trueTeamPointSpawn;
    [SerializeField] private Transform _falsTeamPointSpawn;

    private int _roundCout = 0;
    private HashSet<Player> _playersInRoom = new();
    private HashSet<Player> _playersInRound;
    private bool _teamChoice;
    private bool _TeamChoice => _teamChoice = !_teamChoice;
    private int _trueTeamPoint;
    private int _falsTeamPoint;

    private RoomsPlacer _roomPlacer;

    private void Awake()
    {
        Debug.Log("[GameManager] Awake");
        Instance = this;
    }

    protected override void OnSpawned()
    {
        Debug.Log($"[GameManager] OnSpawned | isServer={isServer}");
        if (isServer)
        {
            _roomPlacer = RoomsPlacer.Instance;
            Debug.Log($"[GameManager] RoomsPlacer assigned : {(_roomPlacer != null ? "OK" : "NULL")}");
        }
    }

    // ─── AddPlayer ───────────────────────────────────────────────
    [ServerRpc(requireOwnership: false)]
    public void AddPlayer(Player pPlayer)
    {
        Debug.Log($"[GameManager][Server] AddPlayer called | player={pPlayer.name} | roomSize={_playersInRoom.Count} | maxPlayers={_roomPlacer?.MaxPlayers}");

        if (IsFull())
        {
            Debug.Log($"[GameManager][Server] Room is full, exiting player {pPlayer.name}");
            _roomPlacer.ExitRoom(pPlayer.PlayerID);
            return;
        }

        bool team = _TeamChoice;
        Debug.Log($"[GameManager][Server] Assigning team={team} to {pPlayer.name}");
        SetTeam(pPlayer, team);

        _playersInRoom.Add(pPlayer);
        Debug.Log($"[GameManager][Server] Player added | roomSize={_playersInRoom.Count}");

        SetActivePlayer(pPlayer, false);
        Debug.Log($"[GameManager][Server] Player {pPlayer.name} set inactive");

        if (!IsFull())
        {
            Debug.Log("[GameManager][Server] Room not full yet, waiting for more players");
            return;
        }

        Debug.Log("[GameManager][Server] Room is full, starting game");
        SetRoom();
        PlayRoom();
    }

    // ─── SetTeam ─────────────────────────────────────────────────
    [ObserversRpc(runLocally: true)]
    private void SetTeam(Player pPlayer, bool pTeam)
    {
        Debug.Log($"[GameManager][Observers] SetTeam | player={pPlayer.name} | team={pTeam}");
        pPlayer.team = pTeam;
    }

    // ─── SetRoom ─────────────────────────────────────────────────
    [ObserversRpc(runLocally: true)]
    private void SetRoom()
    {
        Debug.Log($"[GameManager][Observers] SetRoom | copying {_playersInRoom.Count} players to _playersInRound");
        _playersInRound = new HashSet<Player>(_playersInRoom);
        Debug.Log($"[GameManager][Observers] _playersInRound size={_playersInRound.Count}");
    }

    // ─── PlayRoom ────────────────────────────────────────────────
    [ObserversRpc(runLocally: true)]
    private void PlayRoom()
    {
        Debug.Log($"[GameManager][Observers] PlayRoom | _playersInRound={(_playersInRound == null ? "NULL" : _playersInRound.Count.ToString())}");

        if (_playersInRound == null)
        {
            Debug.LogError("[GameManager][Observers] PlayRoom called but _playersInRound is NULL !");
            return;
        }

        foreach (Player p in _playersInRound)
        {
            Debug.Log($"[GameManager][Observers] Activating player {p.name}");
            SetActivePlayer(p, true);
        }
    }

    // ─── Kill ────────────────────────────────────────────────────
    [ServerRpc(requireOwnership: false)]
    public void Kill(Player pPlayer)
    {
        Debug.Log($"[GameManager][Server] Kill called | player={pPlayer.name}");

        if (_playersInRound == null)
        {
            Debug.LogError("[GameManager][Server] Kill called but _playersInRound is NULL !");
            return;
        }

        if (!_playersInRound.Contains(pPlayer))
        {
            Debug.LogWarning($"[GameManager][Server] Kill : player {pPlayer.name} not in _playersInRound");
            return;
        }

        _playersInRound.Remove(pPlayer);
        Debug.Log($"[GameManager][Server] Player {pPlayer.name} removed | remaining={_playersInRound.Count}");

        SetActivePlayer(pPlayer, false);

        bool allDead = AllPlayerTeamDead(pPlayer.team);
        Debug.Log($"[GameManager][Server] AllPlayerTeamDead(team={pPlayer.team}) = {allDead}");

        if (!allDead) return;

        EndRound(!pPlayer.team);
    }

    // ─── EndRound ────────────────────────────────────────────────
    private void EndRound(bool pVictoryTeam)
    {
        _roundCout++;
        Debug.Log($"[GameManager][Server] EndRound | victoryTeam={pVictoryTeam} | round={_roundCout}/{_nRounds}");

        if (pVictoryTeam)
        {
            _trueTeamPoint++;
            Debug.Log($"[GameManager][Server] True team wins the round | score true={_trueTeamPoint} fals={_falsTeamPoint}");
        }
        else
        {
            _falsTeamPoint++;
            Debug.Log($"[GameManager][Server] Fals team wins the round | score true={_trueTeamPoint} fals={_falsTeamPoint}");
        }

        if (_roundCout >= _nRounds)
        {
            Debug.Log("[GameManager][Server] All rounds done, calling Victory");
            Victory(_trueTeamPoint > _falsTeamPoint);
            return;
        }

        Debug.Log("[GameManager][Server] Starting next round");
        SetRoom();
    }

    // ─── Victory ─────────────────────────────────────────────────
    private void Victory(bool pVictoryTeam)
    {
        Debug.Log($"[GameManager][Server] Victory | winner={(pVictoryTeam ? "True team" : "Fals team")}");
        Debug.Log($"[GameManager][Server] Final score | true={_trueTeamPoint} fals={_falsTeamPoint}");
    }

    // ─── SetActivePlayer ─────────────────────────────────────────
    [ObserversRpc(runLocally: true)]
    private void SetActivePlayer(Player pPlayer, bool pState)
    {
        Debug.Log($"[GameManager][Observers] SetActivePlayer | player={pPlayer.name} | state={pState}");
        pPlayer.gameObject.SetActive(pState);
    }

    // ─── Helpers ─────────────────────────────────────────────────
    private bool IsFull()
    {
        bool full = _playersInRoom.Count >= _roomPlacer.MaxPlayers;
        Debug.Log($"[GameManager] IsFull={full} | {_playersInRoom.Count}/{_roomPlacer.MaxPlayers}");
        return full;
    }

    private bool AllPlayerTeamDead(bool pTeam)
    {
        foreach (Player p in _playersInRound)
            if (p.team == pTeam) return false;
        return true;
    }
}