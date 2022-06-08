using Artanim.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Artanim
{
	public class ScreenshotController : SingletonBehaviour<ScreenshotController>
	{
		private const int SCREENSHOT_DEPTH = 24;

		/// <summary>
		/// Takes a screenshot of the given camera and stores it in the configured format and resolution.
		/// </summary>
		/// <param name="screenshotLabel">Custom label used in the image file name</param>
		/// <param name="camera">Camera to take the screenshot from. If no camera is given, the player main camera (HMD view) is used.</param>
		public void TakeScreenshot(string screenshotLabel, Camera camera = null)
		{
			//Check if session is valid
			if (GameController.Instance.CurrentSession == null)
			{
				Debug.LogWarning("Cannot take a screenshot without a session. Screenshot capture is stopped.");
				return;
			}

			if (!camera)
			{
                //Player view screenshot
                camera = MainCameraController.Instance.PlayerCamera.GetComponent<Camera>();
			}

			if(!camera)
			{
				Debug.LogErrorFormat("Failed to create screenshot. No camera set and player camera could not be found.");
				return;
			}

			if (string.IsNullOrEmpty(screenshotLabel))
				screenshotLabel = "Unknown";

			var screenshotFile = GetScreenshotFilePath(screenshotLabel);

			if(string.IsNullOrEmpty(screenshotFile))
			{
				Debug.LogErrorFormat("Failed to create screenshot. Invalid target file path.");
			}
			else
			{
				//Take hires screenshot
				var clientConfig = ConfigService.Instance.Config.Location.Client;
				var renderTexture = RenderTexture.GetTemporary(clientConfig.ScreenshotWidth, clientConfig.ScreenshotHeight, SCREENSHOT_DEPTH);
				var prevRT = camera.targetTexture;
				try
				{
					camera.targetTexture = renderTexture;
					camera.Render();
					RenderTexture.active = renderTexture;
					var screenshot = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
					screenshot.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
					RenderTexture.active = null;

					byte[] bytes;
					if (ConfigService.Instance.Config.Location.Client.ScreenshotFileType == Location.Config.EScreenshotFileType.JPG)
						bytes = screenshot.EncodeToJPG();
					else
						bytes = screenshot.EncodeToPNG();

					//Ensure directory is present
					Directory.CreateDirectory(Path.GetDirectoryName(screenshotFile));

					File.WriteAllBytes(screenshotFile, bytes);
					Debug.LogFormat("Stored new screenshot to: {0}", screenshotFile);

				}
				finally
				{
					camera.targetTexture = prevRT;
					RenderTexture.active = null;
					renderTexture.Release();
				}
			}
        }

		private string GetScreenshotFilePath(string cameraTag)
		{
			//Create filename
			string extension = ConfigService.Instance.Config.Location.Client.ScreenshotFileType.ToString().ToLowerInvariant();
			string fileName = string.Format("{0}-{1}.{2}", DateTime.Now.ToString(), cameraTag, extension);

			//Get path
			return PathUtils.GetSessionFilePathname(fileName);
		}

	}
}