using PurrNet;
using UnityEngine;
using UnityEngine.UI;

public class RoomTester : NetworkBehaviour
{
    [SerializeField] private Image _backGround;

    protected override void OnSpawned(bool asServer)
    {
        SetColor();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) SetColor();
    }

    [ServerRpc] 
    private void SetColor()
    {
        ApplyColor(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
    }

    [ObserversRpc] 
    private void ApplyColor(float r, float g, float b)
    {
        _backGround.color = new Color(r, g, b);
    }
}