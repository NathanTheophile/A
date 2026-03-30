using UnityEngine;
using PurrNet;

public class PlayerExtendedCollider : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Player _Player;
    [SerializeField] private ShieldCollider _ShieldCollider;
    [SerializeField] private Transform _AnchorPoint;

    [Header("Curve")]
    [SerializeField, Min(3)] private int _curveResolution = 12;
    [SerializeField, Min(0f)] private float _curveAmplitude = 1.2f;
    [SerializeField] private bool _useSinusCurve = true;

    private void Awake()
    {
        if (_ShieldCollider == null)
            _ShieldCollider = GetComponentInParent<ShieldCollider>();

        if (_AnchorPoint == null && _ShieldCollider != null)
            _AnchorPoint = _ShieldCollider.BallAnchor;
    }

    // quand la balle entre dans le premier cercle du
    private void OnTriggerEnter(Collider other)
    {
        if (_Player != null && !_Player.isOwner)
            return;

        LogicBall ball = other.GetComponent<LogicBall>();
        if (ball == null) return;

        Transform anchor = _AnchorPoint;
        if (anchor == null && _ShieldCollider != null)
            anchor = _ShieldCollider.BallAnchor;

        if (anchor == null)
            return;

        ball.RequestCurveToAnchor(anchor.position, _curveResolution, _curveAmplitude, _useSinusCurve);
    }

    // si le joueur se déplace et que la balle ressort du cercle de détection, elle repart vers son forward.
    void OnTriggerExit(Collider other)
    {
        // Intentionally left empty for now.
    }
}
