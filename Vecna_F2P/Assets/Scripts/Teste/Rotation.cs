using PurrNet;
using UnityEngine;
using static UnityEngine.Tilemaps.Tilemap;

public class Rotation : NetworkBehaviour
{
    [SerializeField] private float speed = 90f;

    // S'exécute une seule fois quand l'objet est prêt sur le réseau
    protected override void OnSpawned()
    {
        Debug.Log($"Objet spawné ! IsServer: {isServer}");
    }

    private void Update()
    {
        // Seul le serveur calcule la rotation
        if (!isServer) return;
        // Sur le client, pour afficher la valeur synchronisée
        transform.Rotate(Vector3.up * speed * Time.deltaTime);
    }
}