using PurrNet;
using UnityEngine;

public class ShieldCollider : NetworkBehaviour
{
    [SerializeField] private Player _Player;
    [SerializeField] private LayerMask _ShieldLayer;

    private void OnValidate()
    {
        Collider shieldCollider = GetComponent<Collider>();
        if (shieldCollider != null && (_ShieldLayer.value & (1 << shieldCollider.gameObject.layer)) == 0)
        {
            Debug.LogWarning($"Le layer du shield '{LayerMask.LayerToName(shieldCollider.gameObject.layer)}' n'est pas dans _ShieldLayer.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer)
            return;

        LogicBall lBall = other.GetComponent<LogicBall>();
        if (lBall == null)
            return;
    }
}
