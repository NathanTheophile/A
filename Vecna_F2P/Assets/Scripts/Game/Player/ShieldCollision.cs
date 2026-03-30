using UnityEngine;
using PurrNet;
using Edgegap.Editor;

public class ShieldCollision : NetworkBehaviour
{
    [SerializeField] private Player _Player;
    [SerializeField] private LayerMask _ShieldLayer;
    [SerializeField, Min(0.01f)] private float _ImpactRayDistance = 3f;
    [SerializeField] private string _ShieldTag = "Shield";

    private void OnTriggerEnter(Collider other)
    {
        LogicBall lBall = other.GetComponent<LogicBall>();
        if (lBall == null) return;

        Vector3 lStartPos = lBall.transform.position;
        Vector3 lDirectionNormalized = lBall.transform.forward.normalized;

        if (Physics.Raycast(lStartPos, lDirectionNormalized, out RaycastHit hit, _ImpactRayDistance, _ShieldLayer))
        {
            ProcessImpact(lBall, lStartPos, hit.point, hit.normal);
            return;
        }

        Collider shieldCollider = GetComponent<Collider>();
        if (!IsValidShieldCollider(shieldCollider))
        {
            Debug.LogWarning($"Shield collider '{name}' n'a ni le layer ni le tag attendu pour un fallback d'impact.");
            return;
        }

        if (!LayerMatchesMask(shieldCollider.gameObject.layer, _ShieldLayer))
        {
            Debug.LogWarning($"Le layer du shield '{LayerMask.LayerToName(shieldCollider.gameObject.layer)}' n'est pas inclus dans _ShieldLayer.");
        }

        Vector3 lFallbackHitPoint = shieldCollider.ClosestPoint(lStartPos);
        Vector3 lFallbackNormal = (lStartPos - lFallbackHitPoint).normalized;
        if (lFallbackNormal.sqrMagnitude < Mathf.Epsilon)
        {
            lFallbackNormal = -lDirectionNormalized;
        }

        ProcessImpact(lBall, lStartPos, lFallbackHitPoint, lFallbackNormal);
    }

    private void ProcessImpact(LogicBall ball, Vector3 startPos, Vector3 hitPoint, Vector3 hitNormal)
    {
        Debug.Log("J'ai hit le shield");
        Vector3 lIncomingVec = hitPoint - startPos;
        Vector3 lReflectVec = Vector3.Reflect(lIncomingVec, hitNormal);

        Debug.DrawLine(startPos, hitPoint, Color.red);
        Debug.DrawRay(hitPoint, lReflectVec * 100f, Color.green);
        ball.RequestNewTrajectoryRpc(startPos, lReflectVec);
    }

    private bool IsValidShieldCollider(Collider shieldCollider)
    {
        if (shieldCollider == null)
        {
            return false;
        }

        bool lLayerIsValid = LayerMatchesMask(shieldCollider.gameObject.layer, _ShieldLayer);
        bool lTagIsValid = !string.IsNullOrWhiteSpace(_ShieldTag) && shieldCollider.tag == _ShieldTag;
        return lLayerIsValid || lTagIsValid;
    }

    private static bool LayerMatchesMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}
