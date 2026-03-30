using PurrNet;
using UnityEngine;

public class ShieldCollision : NetworkBehaviour
{
    [SerializeField] private Player _Player;
    [SerializeField] private LayerMask _ShieldLayer;
    [SerializeField, Min(0.01f)] private float _ImpactRayDistance = 3f;
    [SerializeField] private PlayerShieldImpactController _ImpactController;

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

        Vector3 lStartPos = lBall.transform.position;
        Vector3 lDirection = lBall.transform.forward.normalized;

        if (Physics.Raycast(lStartPos, lDirection, out RaycastHit hit, _ImpactRayDistance, _ShieldLayer))
        {
            ProcessImpact(lBall, lStartPos, hit.point, hit.normal);
            return;
        }

        Collider shieldCollider = GetComponent<Collider>();
        bool isShieldLayer = (_ShieldLayer.value & (1 << gameObject.layer)) != 0;
        bool isShieldTag = gameObject.tag == "Shield";
        if (shieldCollider == null || (!isShieldLayer && !isShieldTag))
            return;

        Vector3 lFallbackPoint = shieldCollider.ClosestPoint(lStartPos);
        Vector3 lFallbackNormal = (lStartPos - lFallbackPoint).normalized;
        if (lFallbackNormal.sqrMagnitude < Mathf.Epsilon)
            lFallbackNormal = -lDirection;

        ProcessImpact(lBall, lStartPos, lFallbackPoint, lFallbackNormal);
    }

    private void ProcessImpact(LogicBall ball, Vector3 startPos, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (_ImpactController == null)
        {
            Debug.LogWarning("PlayerShieldImpactController manquant sur ShieldCollision.");
            return;
        }

        _ImpactController.TryHandleImpact(ball, startPos, hitPoint, hitNormal);
    }
}
