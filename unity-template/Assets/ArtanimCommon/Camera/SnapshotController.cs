using Artanim.Location.Messages;
using Artanim.Location.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Artanim
{
    /// <summary>
    /// Renders and send a screenshot when receiving network message <see cref="RequestTakeSnapshot"/>
    /// </summary>
    /// <remarks>This class should be named "ScreenshotController"</remarks>
	public class SnapshotController : MonoBehaviour
	{
        private const FilterMode FILTER_MODE = FilterMode.Bilinear;
        private const TextureFormat TEXTURE_FORMAT = TextureFormat.ARGB32;

        private Texture2D _screenshotTex;

        void Start()
        {
            _screenshotTex = new Texture2D(1, 1, TEXTURE_FORMAT, false);
        }

        void OnEnable()
		{
			NetworkInterface.Instance.Subscribe<RequestTakeSnapshot>(OnRequestTakeSnapshot);
            if (NetworkInterface.Instance.IsServer)
                NetworkInterface.Instance.Subscribe<RequestTakeCalibrationSnapshot>(OnRequestTakeCalibrationSnapshot);
		}

		void OnDisable()
		{
		    NetworkInterface.SafeUnsubscribe<RequestTakeSnapshot>(OnRequestTakeSnapshot);
		    NetworkInterface.SafeUnsubscribe<RequestTakeCalibrationSnapshot>(OnRequestTakeCalibrationSnapshot);
		}

        private void OnRequestTakeSnapshot(RequestTakeSnapshot args)
        {
            //Adjust texture size
            ResizeScreenshotTexture(args.Width, args.Height);

            //Find camera
            Camera camera = NetworkInterface.Instance.IsClient
                ? MainCameraController.Instance.ActiveCamera.GetComponent<Camera>()
                : null;

            if (camera)
            {
                //Directly render camera to low res texture without screenspace UI
                TakeCameraSnapshot(camera, _screenshotTex);
            }
            else
            {
                //Fallback using full screenshot but with visible screenspace UI
                _screenshotTex.ReadPixels(new Rect(0, 0, _screenshotTex.width, _screenshotTex.height), 0, 0, false);
                _screenshotTex.Apply();
            }

            //Compress and send snapshot
            SendSnapshot(args.SenderId, args.JpgQuality);
        }

        private void OnRequestTakeCalibrationSnapshot(RequestTakeCalibrationSnapshot args)
        {
            //Adjust texture size
            ResizeScreenshotTexture(args.ProjectionConfig.Screenshot.Width, CalibrationViewCamera.ComputeScreenshotWidth(args.ProjectionConfig));

            //Get camera
            var camera = CalibrationViewCamera.Instance.GetAndConfigureCamera(args.ProjectionConfig);

            //Directly render camera to low res texture without screenspace UI
            TakeCameraSnapshot(camera, _screenshotTex);

            //Compress and send snapshot
            SendSnapshot(args.SenderId, args.ProjectionConfig.Screenshot.JpegQuality);
        }

        private void ResizeScreenshotTexture(int width, int height)
        {
            //Resize screenshot texture if needed
            if ((_screenshotTex.width != width) || (_screenshotTex.height != height))
            {
                // screen size has changed. Resize our texture object.
                _screenshotTex.Reinitialize(width, height);
                _screenshotTex.Apply();
            }
        }

        private void TakeCameraSnapshot(Camera camera, Texture2D target)
        {
            bool cameraEnabled = camera.enabled;
            camera.enabled = true;

            var prevCamRT = camera.targetTexture;
            var rt = RenderTexture.GetTemporary(target.width, target.height, 24, RenderTextureFormat.ARGB32);
            try
            {
                rt.filterMode = target.filterMode = FILTER_MODE;
                camera.targetTexture = rt;
                RenderTexture.active = rt;
                camera.Render();
                target.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0, false);
                target.Apply();
            }
            finally
            {
                camera.targetTexture = prevCamRT;
                RenderTexture.active = null;
                rt.Release();

                camera.enabled = cameraEnabled;
            }
        }

        private void SendSnapshot(Guid recipientId, int snapshotQuality)
        {
            //Convert to jpg buffer and store in network message
            var msg = new SnapshotResult
            {
                RecipientId = recipientId,
                Width = _screenshotTex.width,
                Height = _screenshotTex.height,
                JpgBuffer = _screenshotTex.EncodeToJPG(snapshotQuality),
            };

            //Send message
            NetworkInterface.Instance.SendMessage(msg);
            Debug.LogFormat("Send snapshot at {0}x{1} compressed to {2} bytes", msg.Width, msg.Height, msg.JpgBuffer.Length);
        }
    }
}