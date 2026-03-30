#region _____________________________/ INFOS
//  AUTHORS : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using PurrNet;
using UnityEngine;

public class BallSpawner : NetworkBehaviour
{
    #region _________________________/ MAIN VALUES
    [Header("Ball")]
    [SerializeField] private GameObject _BallPrefab;

    [Header("Spawn Points")]
    [SerializeField] private SyncArray<Transform> _SpawnPoints;
    private Transform _NextSpawnPoint;

    private SyncVar<bool> _Spawned;

    #endregion

    #region _________________________| SPAWN METHODS
    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        enabled = isOwner;

        DefineNextSpawner();
        Instantiate(_BallPrefab, _NextSpawnPoint);
        _Spawned.value = true;
    }

    private void DefineNextSpawner() {
        int lIndex = Random.Range(0, _SpawnPoints.Length - 1);
        _NextSpawnPoint = _SpawnPoints[lIndex]; }

    #endregion
}