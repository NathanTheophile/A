using UnityEngine;
using PurrNet;
using System;
using Com.IsartDigital.F2P.Player.PlayerProfiles;
public class Player : NetworkBehaviour
{
    [SerializeField] private PlayerAchitecture _PlayerProfile;

    [Header("Setting")]
    [SerializeField] private int _MaxHealt = 1;
    private GameManager _GameManager;
    public PlayerID PlayerID {  get; private set; }
    public bool team;

    private int _healt;
    private int _Healt
    {
        get { return _healt; }
        set{_healt = value; }
    }
    private void Awake()
    {
        PlayerID = InstanceHandler.NetworkManager.localPlayer;
        _Healt = _MaxHealt;
    }
    protected override void OnSpawned()
    {
        base.OnSpawned();
        enabled = isOwner;
        _GameManager = GameManager.Instance;
        if(isOwner)_GameManager.AddPlayer(this);
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.K))
        {
            Deads();
        }
    }
    public void HitDamage()
    {
        _Healt--;
        Debug.Log(_Healt + " Take Damage");
        if (_Healt <= 0) Deads();
    }
    public void Deads()
    {
        _GameManager.Kill(this);
    }
}
