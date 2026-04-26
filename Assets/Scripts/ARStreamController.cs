using System.Collections.Generic;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityGLTF;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ARStreamController : MonoBehaviour
{
    [Header("AR Components")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;


    [Header("Model Streaming")]
    [Tooltip("Paste your raw GitHub .glb link here")]
    public string modelUrl = "https://raw.githubusercontent.com/OmKulthe1/Error404/main/japanese_temple.glb";

    [Header("Interaction Limits")]
    public float minScale = 0.1f;
    public float maxScale = 3.0f;
    public float rotationSpeed = 0.2f;

    private GameObject downloadedModel;
    private GameObject placedModel;
    private bool isDownloading = false;
    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Start()
    {
        DownloadAndInstantiateModelAsync();
    }

    private async void DownloadAndInstantiateModelAsync()
    {
        if (isDownloading || downloadedModel != null) return;

        isDownloading = true;
        var gltf = new GltfImport();

        bool success = await gltf.Load(modelUrl);
        if (success)
        {
            downloadedModel = new GameObject("StreamedModel");
            await gltf.InstantiateMainSceneAsync(downloadedModel.transform);
            downloadedModel.SetActive(false);
            Debug.Log("Model downloaded successfully!");
        }
        else
        {
            Debug.LogError("Failed to load model from network.");
        }

        isDownloading = false;
    }

    void Update()
    {
        // Don't do anything if we are still downloading or failed
        if (isDownloading || downloadedModel == null) return;

        // Ensure AR session is tracking before attempting placement/raycast
        if (ARSession.state != ARSessionState.SessionTracking)
        {
            Debug.Log($"ARSession not tracking yet: {ARSession.state}");
            return;
        }

        // Get touches from the new input system
        var touches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;

        // Ensure we are touching the screen
        if (touches.Count > 0)
        {
            Debug.Log($"Touches: {touches.Count} first pos: {touches[0].screenPosition}");
            var touch0 = touches[0];

            if (IsPointerOverUI(touch0.screenPosition))
            {
                Debug.Log("Touch blocked by UI at " + touch0.screenPosition);
                return;
            }

            // --- SINGLE TOUCH: PLACE OR ROTATE ---
            if (touches.Count == 1)
            {
                if (placedModel == null)
                {
                    // TAP TO PLACE
                    if (touch0.phase == UnityEngine.InputSystem.TouchPhase.Began)
                    {
                        Handheld.Vibrate();

                        var queryTypes = TrackableType.PlaneWithinPolygon | TrackableType.FeaturePoint;
                        if (raycastManager.Raycast(touch0.screenPosition, hits, queryTypes))
                        {
                            var hitPose = hits[0].pose;

                            Vector3 offsetPosition = hitPose.position + new Vector3(0, 0.2f, 0);

                            downloadedModel.transform.position = offsetPosition;
                            downloadedModel.transform.rotation = hitPose.rotation;

                            downloadedModel.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                            downloadedModel.SetActive(true);
                            placedModel = downloadedModel;
                        }
                        else
                        {
                            // Helpful debug info to diagnose "No point hit." from ARCore
                            int planeCount = planeManager != null ? planeManager.trackables.count : -1;
                            Debug.Log($"Raycast found no hits. Plane count: {planeCount}. ARSession state: {ARSession.state}");
                        }
                    }
                }
                else
                {
                    // SWIPE TO ROTATE
                    if (touch0.phase == UnityEngine.InputSystem.TouchPhase.Moved)
                    {
                        placedModel.transform.Rotate(0, -touch0.delta.x * rotationSpeed, 0, Space.World);
                    }
                }
            }
            // --- TWO TOUCHES: PINCH TO SCALE ---
            else if (touches.Count == 2 && placedModel != null)
            {
                var touch1 = touches[1];

                if (touch0.phase == UnityEngine.InputSystem.TouchPhase.Moved || touch1.phase == UnityEngine.InputSystem.TouchPhase.Moved)
                {
                    // Calculate distance between touches this frame and last frame
                    float currentDistance = Vector2.Distance(touch0.screenPosition, touch1.screenPosition);
                    float previousDistance = Vector2.Distance(
                        touch0.screenPosition - touch0.delta,
                        touch1.screenPosition - touch1.delta);

                    // Difference determines if we are zooming in or out
                    float scaleDelta = (currentDistance - previousDistance) * 0.005f;

                    Vector3 newScale = placedModel.transform.localScale + new Vector3(scaleDelta, scaleDelta, scaleDelta);

                    // Clamp to min and max limits
                    newScale.x = Mathf.Clamp(newScale.x, minScale, maxScale);
                    newScale.y = Mathf.Clamp(newScale.y, minScale, maxScale);
                    newScale.z = Mathf.Clamp(newScale.z, minScale, maxScale);

                    placedModel.transform.localScale = newScale;
                }
            }
        }
    }

    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        // Guard if there's no EventSystem in the scene (prevents null refs)
        if (EventSystem.current == null) return false;

        var eventData = new PointerEventData(EventSystem.current) { position = screenPosition };
        var results = new System.Collections.Generic.List<RaycastResult>();
        // Raycast against all UI (requires EventSystem + GraphicRaycaster on canvases)
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }


}