using Artanim.Tracking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Artanim
{

[RequireComponent(typeof(TrackingRigidbody))]
public class RigidbodyDisplay : MonoBehaviour, ISelectable
{
	private static Vector3 CANVAS_OFFSET = new Vector3(0f, 0.3f, 0f);

	public Canvas CanvasDisplay;
	public Text TextRigidbodyName;

	private TrackingRigidbody TrackingRigidbody;

	void Start ()
	{
		TrackingRigidbody = GetComponent<TrackingRigidbody>();

		if (CanvasDisplay)
			CanvasDisplay.gameObject.SetActive(false);
	}

	void Update()
	{
		if(CanvasDisplay && CanvasDisplay.gameObject.activeInHierarchy)
		{
			CanvasDisplay.transform.position = Vector3.MoveTowards(transform.position + CANVAS_OFFSET, Camera.main.transform.position, 0.2f);
			CanvasDisplay.transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
		}
	}

	public void Select()
	{
		if (CanvasDisplay && TrackingRigidbody && TextRigidbodyName)
		{
			TextRigidbodyName.text = TrackingRigidbody.RigidbodyName;

			CanvasDisplay.gameObject.SetActive(true);
		}
	}

	public void Deselect()
	{
		if(CanvasDisplay)
			CanvasDisplay.gameObject.SetActive(false);
	}

}

}