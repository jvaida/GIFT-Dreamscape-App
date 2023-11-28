using Artanim.Location.Network;
using Artanim.Monitoring;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Artanim
{
    /// <summary>
    /// Renders screenshots and send them using a metrics
    /// </summary>
    public class SnapshotWithMetrics : MonoBehaviour
    {
        private readonly Vector2Int CLIENT_SCREENSHOT_SIZE = new Vector2Int(240 * 2, 135 * 2);
        private const int CLIENT_JPG_QUALITY = 25;
        private const FilterMode FILTER_MODE = FilterMode.Bilinear;
        private const TextureFormat TEXTURE_FORMAT = TextureFormat.RGB24;

        private MetricsRawChannel _metrics;
        private bool _doSnapShot = false;
        // Ensure callback does not get garbage collected
        private MetricsManager.AskSendNowCallbackDelegate _askNowCallback;
        Texture2D _screenshotTex;
        Texture2D _reducedScreenshotTex;

        IEnumerator Start()
        {
            var waitEndOfFrame = new WaitForEndOfFrame();
            while (true)
            {
                yield return waitEndOfFrame;

                if (enabled && _doSnapShot && _metrics != null)
                {
                    DoSnapShot();
                }

                _doSnapShot = false;
            }
        }

        void OnEnable()
        {
            // Create metrics channels
            _metrics = MetricsManager.Instance.GetRawChannelInstance(MetricsAction.Create, "Snapshot", (ulong)CLIENT_SCREENSHOT_SIZE.x * (ulong)CLIENT_SCREENSHOT_SIZE.y * 3);
            _askNowCallback = (m) => { _doSnapShot = true; };
            _metrics.SetAskSendDataNowCallback(_askNowCallback);

            // Create textures
            _screenshotTex = new Texture2D(1, 1, TEXTURE_FORMAT, false);
            _reducedScreenshotTex = new Texture2D(CLIENT_SCREENSHOT_SIZE.x, CLIENT_SCREENSHOT_SIZE.y, TEXTURE_FORMAT, false);
        }

        void OnDisable()
        {
            // Destroy metrics channels
            if (_metrics != null)
            {
                _metrics.Dispose();
                _metrics = null;
            }

            _doSnapShot = false;
        }

        void DoSnapShot()
        {
            IntPtr buffer = _metrics.BeginSend();
            if (buffer != IntPtr.Zero)
            {
                if (_screenshotTex.width != Screen.width || _screenshotTex.height != Screen.height)
                {
                    // screen size has changed. Resize our texture object.
                    _screenshotTex.Reinitialize(Screen.width, Screen.height);
                    _screenshotTex.Apply();
                }

                var camera = MainCameraController.Instance.ActiveCamera.GetComponent<Camera>();
                if (camera)
                {
                    //Directly render camera to low res texture without screenspace UI
                    TakeCameraScreenshot(camera, _reducedScreenshotTex);
                }
                else
                {
                    //Fallback using full screenshot but with visible screenspace UI
                    TakeFullresScreenshot(_screenshotTex);
                    ResizeTexture(_screenshotTex, _reducedScreenshotTex, FILTER_MODE); // Can't get Graphics.ConvertTexture to work
                }

                //Send data
                SendScreenshot(buffer, _reducedScreenshotTex);
            }
        }

        private void TakeCameraScreenshot(Camera camera, Texture2D target)
        {
            bool cameraEnabled = camera.enabled;
            camera.enabled = true;

            var prevCamRT = camera.targetTexture;
            var rt = RenderTexture.GetTemporary(CLIENT_SCREENSHOT_SIZE.x, CLIENT_SCREENSHOT_SIZE.y, 24, RenderTextureFormat.ARGB32);
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

        private void TakeFullresScreenshot(Texture2D texture)
        {
            texture.ReadPixels(new Rect(0, 0, _screenshotTex.width, _screenshotTex.height), 0, 0, false);
            texture.Apply();
        }

        private void ResizeTexture(Texture2D source, Texture2D target, FilterMode filterMode)
        {
            var rt = RenderTexture.GetTemporary(target.width, target.height);
            try
            {
                source.filterMode = rt.filterMode = filterMode;
                RenderTexture.active = rt;
                Graphics.Blit(source, rt);
                target.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                target.Apply();
            }
            finally
            {
                RenderTexture.active = null;
                rt.Release();
            }
        }

        private void SendScreenshot(IntPtr buffer, Texture2D screenshot)
        {
            byte[] jpgData = screenshot.EncodeToJPG(CLIENT_JPG_QUALITY);
            int numBytes = jpgData.Length;
            Marshal.WriteInt64(buffer, numBytes);
            Marshal.Copy(jpgData, 0, new IntPtr(buffer.ToInt64() + 8), numBytes);
            _metrics.EndSend(buffer, (ulong)numBytes + 8);

            Debug.LogFormat("Send snapshot at {0}x{1} compressed to {2} bytes (using metrics)", _reducedScreenshotTex.width, _reducedScreenshotTex.height, numBytes);
        }
    }
}