using System;
using UnityEngine;

[Serializable]
public struct TrajectoryRaySetting
{
    public int nBounce;
    public LayerMask collideLayer;
}
public static class TrajectoryRay
{ 
    public static Vector3[] GetTrajectoryRay(Vector3 pStartPosition,Vector3 pDirection ,TrajectoryRaySetting pSetting) => GetTrajectoryRay(pStartPosition, pDirection ,pSetting.nBounce, pSetting.collideLayer);
    public static Vector3[] GetTrajectoryRay(Vector3 pStartPosition, Vector3 pDirection ,int pNBouce, LayerMask pLayer)
    {
        if (pNBouce <= 0 || pDirection == Vector3.zero)
        {
            Debug.LogWarning("TrajectoryRay is called with parameter NBouce or pDirection <= 0");
            return null;
        }
        pNBouce++;
        Vector3[] lResult = new Vector3[pNBouce];
        lResult[0] = pStartPosition;
        RaycastHit lCurrentHit = default;
        for (int i = 1; i < pNBouce; i++)
        {
            lCurrentHit = AddPoint(lResult[i - 1], pDirection , pLayer);
            pDirection = Vector3.Reflect(pDirection, lCurrentHit.normal);
            lResult[i] = lCurrentHit.point;
        }
        return lResult;
    }
    private static RaycastHit AddPoint(Vector3 pPosition,Vector3 pDirection , LayerMask pLayer)
    {
        if (Physics.Raycast(pPosition, pDirection, out RaycastHit lHit, Mathf.Infinity, pLayer))
            return lHit;

        Debug.LogWarning("Trajectory Ray did not find a collision point to bounce off of");
        return default;
    }
}
