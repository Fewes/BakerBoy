using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class OrbitCamera : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
	public Transform pivot;
	public new Camera camera;
	public new Light light;
	public float rotationSensitivity = 4f;
	public float translationSensitivity = 0.1f;
	public float zoomSensitivity = 5f;

	public GameObject ui;

	bool focused;
	float pitch, yaw;

	Vector3 pivotResetPositon;
	Vector3 cameraResetPosition;
	Vector3 lightResetEuler;

	void Start ()
	{
		pitch = pivot.eulerAngles.x;
		yaw = pivot.eulerAngles.y;
		pivotResetPositon = pivot.position;
		cameraResetPosition = camera.transform.localPosition;
		lightResetEuler = light.transform.eulerAngles;
	}

	public void OnPointerDown (PointerEventData eventData)
    {
        focused = true;
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Confined;
    }

	public void OnPointerUp (PointerEventData eventData)
    {
        focused = false;
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
    }

	void Zoom (float delta)
	{
		var z = -camera.transform.localPosition.z;
		z += delta;
		z = Mathf.Clamp(z, 0, 100);
		camera.transform.localPosition = Vector3.forward * -z;
	}

	void Update ()
	{
		float mouseScroll = Input.GetAxis("Mouse ScrollWheel") * zoomSensitivity;
		if (mouseScroll != 0)
			Zoom(-mouseScroll);

		if (focused)
		{
			float mouseX = Input.GetAxis("Mouse X");
			float mouseY = Input.GetAxis("Mouse Y");

			if (Input.GetMouseButton(0))
			{
				yaw   += mouseX * rotationSensitivity;
				pitch -= mouseY * rotationSensitivity;
			}
			else if (Input.GetMouseButton(1))
			{
				//Zoom(mouseY * zoomSensitivity);
				light.transform.Rotate(camera.transform.up, mouseX * rotationSensitivity, Space.World);
				light.transform.Rotate(camera.transform.right, -mouseY * rotationSensitivity, Space.World);
			}
			else if (Input.GetMouseButton(2))
			{
				pivot.position -= camera.transform.right * mouseX * translationSensitivity;
				pivot.position -= camera.transform.up * mouseY * translationSensitivity;
			}
		}

		if (Input.GetKey(KeyCode.F))
		{
			pivot.position = pivotResetPositon;
			camera.transform.localPosition = cameraResetPosition;
		}

		if (Input.GetKey(KeyCode.R))
		{
			light.transform.eulerAngles = lightResetEuler;
		}

		if (Input.GetKey(KeyCode.Space))
			yaw += 90 * Time.deltaTime;

		ui.SetActive(!Input.GetKey(KeyCode.Space));

		pitch = Mathf.Clamp(pitch, -89, 89);

		pivot.rotation = Quaternion.Euler(pitch, yaw, 0);
	}
}
