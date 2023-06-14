using UnityEngine;
using UnityEngine.UIElements;

public class CameraMovement : MonoBehaviour
{

    private Camera currentCamera;
    private Vector3 previousPosition;
    private float distanceToTarget = 11.5f;
    // Update is called once per frame
    void Update()
    {
        Camera cam = Camera.main;
        if (Input.GetMouseButtonDown(1))
        {
            previousPosition = cam.ScreenToViewportPoint(Input.mousePosition);
        }
        else if (Input.GetMouseButton(1))
        {
            Vector3 newPosition = cam.ScreenToViewportPoint(Input.mousePosition);
            Vector3 direction = previousPosition - newPosition;

            float rotationAroundYAxis = -direction.x * 180; // camera moves horizontally
            float rotationAroundXAxis = direction.y * 180; // camera moves vertically
            
            cam.transform.Rotate(new Vector3(1, 0, 0), rotationAroundXAxis);
            cam.transform.Rotate(new Vector3(0, 1, 0), rotationAroundYAxis, Space.World);

            cam.transform.position = Vector3.zero;
            cam.transform.Translate(new Vector3(0, 0, -distanceToTarget));

            previousPosition = newPosition;
        }

        if (Input.mouseScrollDelta.y > 0 && distanceToTarget > 5)
        {
            distanceToTarget -= 0.5f;
            cam.transform.position = Vector3.zero;
            cam.transform.Translate(new Vector3(0, 0, -distanceToTarget));
        }
        if (Input.mouseScrollDelta.y < 0 && distanceToTarget < 12)
        {
            distanceToTarget += 0.5f;
            cam.transform.position = Vector3.zero;
            cam.transform.Translate(new Vector3(0, 0, -distanceToTarget));
        }
    }
}
