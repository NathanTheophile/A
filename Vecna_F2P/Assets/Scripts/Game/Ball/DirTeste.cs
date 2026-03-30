using UnityEngine;
using UnityEngine.SocialPlatforms;
public class DirTeste : MonoBehaviour
{
    [SerializeField] private LogicBall ball;

    private void Update()
    {
        LookMouse();
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ball?.RequestNewTrajectoryRpc(ball.transform.position, (transform.position - ball.transform.position).normalized);
        }
    }
    private void LookMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        Plane plane = new Plane(Vector3.up, transform.position);

        if (plane.Raycast(ray, out float distance))
        {
            Vector3 targetPoint = ray.GetPoint(distance);
            targetPoint.y = transform.position.y;
            transform.position = (targetPoint);
        }
    }
}
