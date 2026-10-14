using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    public enum Anchor
    {
        GameStart,          // Sits where it is placed in the Scene view when the game starts
        WhenCameraArrives   // Sits where it is placed whenever the camera is centered on it
    }

    [SerializeField] [Tooltip("When this object sits exactly where it is placed in the Scene view\nGameStart:\tat the first frame. Use for big layers (far grove, stew sea)\nWhenCameraArrives:\twhen the camera looks at it. Use for props (carrots, roots)")] private Anchor anchor = Anchor.WhenCameraArrives;
    [SerializeField] [Tooltip("X:\tHorizontal follow fraction\nY:\tVertical follow fraction\n1 = moves with camera (far), 0 = fixed to world, < 0 = foreground")] private Vector2 followFraction = new Vector2(0.2f, 0f);

    private Transform cam;
    private Vector3 startPos;
    private Vector2 referenceCameraPos;

    // Center of every renderer on this object and its children, or the pivot if there are none
    private Vector3 BoundsCenter
    {
        get
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return transform.position;
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
            return bounds.center;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = Camera.main.transform;
        startPos = transform.position;

        CameraFollow follow = cam.GetComponent<CameraFollow>();
        if (anchor == Anchor.GameStart)
            referenceCameraPos = follow ? follow.TargetPos : cam.position;
        else
            referenceCameraPos = follow ? follow.ClampToBounds(BoundsCenter) : BoundsCenter;
    }

    // LateUpdate runs after CameraFollow has moved the camera this frame
    void LateUpdate()
    {
        Vector2 delta = (Vector2)cam.position - referenceCameraPos;
        transform.position = new Vector3(startPos.x + delta.x * followFraction.x, startPos.y + delta.y * followFraction.y, startPos.z);
    }
}
