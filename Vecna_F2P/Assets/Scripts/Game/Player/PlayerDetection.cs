using UnityEngine;
using PurrNet;
using Unity.VisualScripting;

public class PlayerCollider : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Player _Player;
    [SerializeField] private Transform _Shield;
    [SerializeField] private LayerMask _ShieldPlayer;

    [Header("Bezier settings")]
    [SerializeField] private float curveHeight = 1.5f;
    [SerializeField] private float curveSideOffset = 0.75f;
    [SerializeField] private int bezierResolution = 20;

    // quand la balle entre dans le premier cercle du
    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        LogicBall ball = other.GetComponent<LogicBall>();

        if (ball == null) return;

        Vector3 lBallPos = ball.transform.position;
        Vector3 lPlayerPos = _Player.transform.position;
        Vector3 lDirToPlayer = (lPlayerPos - lBallPos).normalized;

        RaycastHit lHit;

        Debug.Log("Je vais déclencher slow");

        //if (isServer) Time.timeScale = .5f;
        SlowMotionTrigger(.5f, ball, .5f);

        Debug.Log("Je vais draw");
        Debug.DrawRay(lBallPos, lDirToPlayer, Color.red, 3f);

        if (Physics.Raycast(lBallPos, lDirToPlayer, out lHit, 3f, _ShieldPlayer)) RedirectBallToShieldCenter(ball);
    }

    // si le joueur se déplace et que la balle ressort du cercle de détection, elle repart vers son forward.
    void OnTriggerExit(Collider other)
    {
        if (!isServer) return;

        LogicBall ball = other.GetComponent<LogicBall>();
        if (ball == null) return;

        //if (isServer) Time.timeScale = 1f;

        SlowMotionTrigger(1f, ball, 2f);

        //ball.RequestNewTrajectoryRpc(ball.transform.position, ball.transform.forward);
    }

private void RedirectBallToShieldCenter(LogicBall ball)
{
    Vector3 start = ball.transform.position;
    Vector3 end = _Shield.position;
    end.y = start.y;

    Vector3 forward = ball.transform.forward;
    forward.y = 0f;
    forward.Normalize();

    Vector3 toEnd = end - start;
    toEnd.y = 0f;

    float distance = toEnd.magnitude;
    if (distance < 0.01f) return;

    Vector3 dirToEnd = toEnd.normalized;

    float sideSign = Mathf.Sign(Vector3.Cross(forward, dirToEnd).y);
    if (Mathf.Abs(sideSign) < 0.01f) sideSign = 1f;

    Vector3 side = Vector3.Cross(Vector3.up, dirToEnd).normalized;

    Vector3 lead = start + forward * (distance * 0.15f);
    Vector3 control = start + forward * (distance * 0.45f) + side * sideSign * curveSideOffset;

    Vector3[] curve = GenerateQuadraticBezier(lead, control, end, bezierResolution);

    Vector3[] finalPath = new Vector3[curve.Length + 1];
    finalPath[0] = start;

    for (int i = 0; i < curve.Length; i++)
        finalPath[i + 1] = curve[i];

    ball.SetCustomTrajectory(finalPath);
}

private Vector3[] GenerateQuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, int resolution)
{
    resolution = Mathf.Max(2, resolution);

    Vector3[] path = new Vector3[resolution + 1];

    for (int i = 0; i <= resolution; i++)
    {
        float t = i / (float)resolution;
        path[i] = EvaluateQuadraticBezier(p0, p1, p2, t);
    }

    return path;
}

private Vector3 EvaluateQuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
{
    float u = 1f - t;
    return u * u * p0 + 2f * u * t * p1 + t * t * p2;
}

    [ObserversRpc]
    private void SlowMotionTrigger(float pSpeed, LogicBall pBall, float value) { /*if(isServer) pBall.moveSpeed = pBall.moveSpeed * value; Debug.Log("Je déclenche le slow"); Time.timeScale = pSpeed;*/}
}