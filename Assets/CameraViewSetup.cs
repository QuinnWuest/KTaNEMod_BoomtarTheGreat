using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraViewSetup : MonoBehaviour
{
    public Transform entryPortal;
    public Transform exitPortal;

    private float fixedFov = 20f;
    private float depthOffset = 0.6f;
    private float horizontalParallax = 0.35f;
    private float verticalParallax = 0.25f;
    private float maxHorizontalOffset = 0.45f;
    private float maxVerticalOffset = 0.30f;
    private float rotationStrength = 1f;

    private Camera portalCam;
    private Camera viewerCamera;

    private void Awake()
    {
        portalCam = GetComponent<Camera>();
    }

    private void Start()
    {
        viewerCamera = Camera.main;

        if (viewerCamera == null)
        {
            Debug.LogError("CameraViewSetup: No Camera.main found.");
            return;
        }

        portalCam.nearClipPlane = 0.01f;
        portalCam.farClipPlane = 100f;
        portalCam.fieldOfView = fixedFov;
    }

    private void LateUpdate()
    {
        if (viewerCamera == null)
            viewerCamera = Camera.main;

        if (viewerCamera == null || entryPortal == null || exitPortal == null)
            return;

        // Viewer relative to the entry portal
        Vector3 viewerLocalPos = entryPortal.InverseTransformPoint(viewerCamera.transform.position);
        Quaternion viewerLocalRot = Quaternion.Inverse(entryPortal.rotation) * viewerCamera.transform.rotation;

        // 180° flip through the portal
        Quaternion halfTurn = Quaternion.Euler(0f, 180f, 0f);
        viewerLocalPos = halfTurn * viewerLocalPos;
        viewerLocalRot = halfTurn * viewerLocalRot;

        // Use mostly X/Y parallax; keep Z mostly fixed so the room stays in frame
        float offsetX = Mathf.Clamp(viewerLocalPos.x * horizontalParallax, -maxHorizontalOffset, maxHorizontalOffset);
        float offsetY = Mathf.Clamp(viewerLocalPos.y * verticalParallax, -maxVerticalOffset, maxVerticalOffset);

        // Put camera a fixed distance "behind" the exit portal in its local space.
        // Depending on your anchor orientation, this may need to be +depthOffset instead of -depthOffset.
        Vector3 exitLocalCameraPos = new Vector3(offsetX, offsetY, -depthOffset);

        Vector3 targetPosition = exitPortal.TransformPoint(exitLocalCameraPos);

        // Reduce rotation influence a lot to avoid the room whipping around
        Quaternion softenedLocalRot = Quaternion.Slerp(Quaternion.identity, viewerLocalRot, rotationStrength);
        Quaternion targetRotation = exitPortal.rotation * softenedLocalRot;

        transform.position = targetPosition;
        transform.rotation = targetRotation;

        portalCam.fieldOfView = fixedFov;
    }
}