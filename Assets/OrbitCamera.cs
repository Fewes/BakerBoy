using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class OrbitCamera : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
	public Transform pivot;
	public float sensitivity = 2f;

	public GameObject ui;

	bool rotate;
	float pitch, yaw;

	void Start ()
	{
		pitch = pivot.eulerAngles.x;
		yaw = pivot.eulerAngles.y;
	}

	public void OnPointerDown (PointerEventData eventData)
    {
        rotate = true;
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Confined;
    }

	public void OnPointerUp (PointerEventData eventData)
    {
        rotate = false;
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
    }

	void Update ()
	{
		if (rotate)
		{
			yaw   += Input.GetAxis("Mouse X") * sensitivity;
			pitch -= Input.GetAxis("Mouse Y") * sensitivity;
		}

		if (Input.GetKey(KeyCode.Space))
			yaw += 90 * Time.deltaTime;

		ui.SetActive(!Input.GetKey(KeyCode.Space));

		pitch = Mathf.Clamp(pitch, -89, 89);

		pivot.rotation = Quaternion.Euler(pitch, yaw, 0);
	}
}
