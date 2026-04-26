using UnityEngine;

public class TouchLook : MonoBehaviour
{
    public float sensitivityX = 0.15f;
    public float sensitivityY = 0.1f;

    float rotationX = 0f;
    float rotationY = 0f;

    void Update()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Moved)
            {
                rotationX += touch.deltaPosition.x * sensitivityX;
                rotationY -= touch.deltaPosition.y * sensitivityY;

                rotationY = Mathf.Clamp(rotationY, -80f, 80f);

                transform.localRotation = Quaternion.Euler(rotationY, rotationX, 0f);
            }
        }
    }
}