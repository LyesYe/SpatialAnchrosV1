using UnityEngine;

public class CanvasFollowCamera : MonoBehaviour
{
	public Transform cameraTransform; // Reference to the camera
	public Vector3 offset; // Offset from the camera

	void Update()
	{
		if (cameraTransform != null)
		{
			// Update the Canvas position to follow the camera
			transform.position = cameraTransform.position + cameraTransform.rotation * offset;
			transform.rotation = cameraTransform.rotation;
		}
	}
}