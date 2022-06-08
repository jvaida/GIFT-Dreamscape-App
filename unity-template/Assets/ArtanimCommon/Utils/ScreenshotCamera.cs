using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Artanim
{
	[AddComponentMenu("Artanim/Screenshot Camera")]
	public class ScreenshotCamera : MonoBehaviour
	{
		private const string SCREENSHOT_NAME_FORMAT = "{0}_{1}_{2:0000}.{3}";

		[Tooltip("Camera used for screenshots and preview")]
		public Camera Camera;

		[Tooltip("Camera preview update rate. If set to 1, the preview is updates every frame.")]
		[Range(1, 60)]
		public int PreviewUpdateRate = 10;

		[Tooltip("Camera name used for the screenshot name. If not set, the GameObject name is used.")]
		public string CameraTag;

		[Header("Render Textures")]
		[Tooltip("RenderTexture used for the preview image on the camera")]
		public RenderTexture PreviewRenderTexture;

		private void Awake()
		{
			// Only on client
			if (!Location.Network.NetworkInterface.Instance.IsClient)
            {
				enabled = false;
			}
			else if (Camera)
			{
				Camera.targetTexture = PreviewRenderTexture;
				Camera.enabled = false;
			}
		}
		
		private void Update()
		{
			//Update preview
			if (PreviewRenderTexture && Time.frameCount % PreviewUpdateRate == 0)
			{
				Camera.Render();
			}
		}

		public void DoTakeScreenshot()
		{
			if(ScreenshotController.Instance)
				ScreenshotController.Instance.TakeScreenshot(!string.IsNullOrEmpty(CameraTag) ? CameraTag : gameObject.name, Camera);
		}
		
	}
}