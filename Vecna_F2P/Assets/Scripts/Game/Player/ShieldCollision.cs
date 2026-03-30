using UnityEngine;
using PurrNet;
using Edgegap.Editor;

public class ShieldCollision : NetworkBehaviour
{
    [SerializeField] private Player _Player;
    [SerializeField] private LayerMask _ShieldLayer;

    private void OnTriggerEnter(Collider other)
    {
        LogicBall lBall = other.GetComponent<LogicBall>();
        if (lBall == null) return;

        Vector3 lStartPos = lBall.transform.position;

        RaycastHit hit;

        if (Physics.Raycast(lStartPos, lBall.transform.forward * 3f, out hit, _ShieldLayer))
        {
            Debug.Log("J'ai hit le shield");
            Vector3 lIncomingVec = hit.point - lStartPos;

            Vector3 lReflectVec = Vector3.Reflect(lIncomingVec, hit.normal);

            Debug.DrawLine(lStartPos, hit.point, Color.red);
            Debug.DrawRay(hit.point, lReflectVec * 100, Color.green);
            lBall.RequestNewTrajectoryRpc(lStartPos, lReflectVec);
        }
    }
}