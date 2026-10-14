using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private float followSpeed = 0.1f;
    [SerializeField] private Vector3 offset;
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;

    // Where the camera is heading: the player plus offset, clamped to the bounds (z keeps the camera's own z)
    public Vector3 TargetPos => ClampToBounds(PlayerController.Instance.transform.position + offset);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Snap onto the player so the first frame already shows the start area
        transform.position = TargetPos;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, TargetPos, followSpeed);
    }

    public Vector3 ClampToBounds(Vector3 pos) => new Vector3(Mathf.Clamp(pos.x, minBounds.x, maxBounds.x), Mathf.Clamp(pos.y, minBounds.y, maxBounds.y), transform.position.z);
}
