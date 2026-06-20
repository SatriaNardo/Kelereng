using UnityEngine;

[RequireComponent(typeof(EdgeCollider2D))]
public class ScreenBoundaries : MonoBehaviour
{
    [Header("Physics Settings")]
    public PhysicsMaterial2D bounceMaterial; // Drag your custom high-bounce material here

    private EdgeCollider2D edgeCollider;

    private void Start()
    {
        edgeCollider = GetComponent<EdgeCollider2D>();
        
        if (bounceMaterial != null)
        {
            edgeCollider.sharedMaterial = bounceMaterial;
        }

        CreateScreenEdges();
    }

    private void CreateScreenEdges()
    {
        // Get the Main Camera bounds in world coordinates
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector2 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
        Vector2 topRight = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));
        Vector2 topLeft = new Vector2(bottomLeft.x, topRight.y);
        Vector2 bottomRight = new Vector2(topRight.x, bottomLeft.y);

        // Define the 5 points that make a closed loop around the screen
        Vector2[] edgePoints = new Vector2[5];
        edgePoints[0] = bottomLeft;
        edgePoints[1] = topLeft;
        edgePoints[2] = topRight;
        edgePoints[3] = bottomRight;
        edgePoints[4] = bottomLeft; // Closes the loop

        edgeCollider.points = edgePoints;
    }
}