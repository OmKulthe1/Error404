using System.Collections.Generic;
using GLTFast;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityGLTF;

public class ARStreamController : MonoBehaviour
{
    [Header("AR Components")]
    public ARRaycastManager raycastManager;

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

    async void Start()
    {
        // 1. Start downloading the model over the network
        isDownloading = true;
        var gltf = new GltfImport();

        bool success = await gltf.Load(modelUrl);
        if (success)
        {
            downloadedModel = new GameObject("StreamedModel");
            await gltf.InstantiateMainSceneAsync(downloadedModel.transform);
            downloadedModel.SetActive(false); // Hide it until the user taps
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

        // Ensure we are touching the screen
        if (Input.touchCount > 0)
        {
            Touch touch0 = Input.GetTouch(0);

            // --- SINGLE TOUCH: PLACE OR ROTATE ---
            if (Input.touchCount == 1)
            {
                if (placedModel == null)
                {
                    // TAP TO PLACE
                    if (touch0.phase == TouchPhase.Began)
                    {
                        if (raycastManager.Raycast(touch0.position, hits, TrackableType.PlaneWithinPolygon))
                        {
                            var hitPose = hits[0].pose;
                            downloadedModel.transform.position = hitPose.position;
                            downloadedModel.transform.rotation = hitPose.rotation;
                            downloadedModel.SetActive(true);
                            placedModel = downloadedModel; // Mark as placed
                        }
                    }
                }
                else
                {
                    // SWIPE TO ROTATE
                    if (touch0.phase == TouchPhase.Moved)
                    {
                        // Rotate around the Y axis based on horizontal swipe
                        placedModel.transform.Rotate(0, -touch0.deltaPosition.x * rotationSpeed, 0, Space.World);
                    }
                }
            }
            // --- TWO TOUCHES: PINCH TO SCALE ---
            else if (Input.touchCount == 2 && placedModel != null)
            {
                Touch touch1 = Input.GetTouch(1);

                if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
                {
                    // Calculate distance between touches this frame and last frame
                    float currentDistance = Vector2.Distance(touch0.position, touch1.position);
                    float previousDistance = Vector2.Distance(touch0.position - touch0.deltaPosition, touch1.position - touch1.deltaPosition);

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
}