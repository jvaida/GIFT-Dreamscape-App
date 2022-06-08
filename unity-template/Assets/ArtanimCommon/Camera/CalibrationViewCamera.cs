using Artanim.Location.Config;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
    /// <summary>
    /// Helper script that configures a camera for rendering screenshots showing only the player's avatars
    /// and from a given view point.
    /// This is used by the game server to render the avatars as shown in the Hostess calibration view.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CalibrationViewCamera : SingletonBehaviour<CalibrationViewCamera>
    {
        Camera _camera;

        /// <summary>
        /// Returns the size of the screenshot (based on the configuration settings)
        /// </summary>
        public static int ComputeScreenshotWidth(CalibrationViewProjectionConfig projectionConfig)
        {
            return Mathf.RoundToInt(projectionConfig.Screenshot.Width * projectionConfig.Size.Y / projectionConfig.Size.X);
        }

        /// <summary>
        /// Configures and returns the camera for rendering avatars screenshots for the Hostess calibration view
        /// </summary>
        /// <returns>The camera for avatars screenshots</returns>
        public Camera GetAndConfigureCamera(CalibrationViewProjectionConfig projectionConfig)
        {
            var players = GameController.Instance.RuntimePlayers;
            if ((players != null) && (players.Count > 0) && (players[0] != null) && (players[0].PlayerInstance != null))
            {
                // Use player's avatar layer for the culling mask
                _camera.cullingMask = 1 << players[0].PlayerInstance.layer;
            }

            SetupCamera(projectionConfig);

            return _camera;
        }

        private void SetupCamera(CalibrationViewProjectionConfig projectionConfig)
        {
            // Setup camera
            float dist = Mathf.Max(projectionConfig.Size.X, projectionConfig.Size.Y);
            Vector2 offset = new Vector2(0.5f * projectionConfig.Size.X - projectionConfig.Offset.X, 0.5f * projectionConfig.Size.Y - projectionConfig.Offset.Y);
            Vector3 pos;
            Quaternion rot;
            switch (projectionConfig.Orientation.Plane)
            {
                default:
                case CalibrationViewProjectionConfig.PlaneName.XY:
                    pos = new Vector3(offset.x, offset.y, -dist);
                    rot = Quaternion.Euler(0, 0, projectionConfig.Orientation.AngleDegrees);
                    break;
                case CalibrationViewProjectionConfig.PlaneName.XZ:
                    pos = new Vector3(offset.x, dist, offset.y);
                    rot = Quaternion.Euler(90, projectionConfig.Orientation.AngleDegrees, 0);
                    break;
                case CalibrationViewProjectionConfig.PlaneName.YZ:
                    pos = new Vector3(dist, offset.x, offset.y);
                    rot = Quaternion.Euler(projectionConfig.Orientation.AngleDegrees, 0, 90);
                    break;
            }

            _camera.transform.localPosition = pos;
            _camera.transform.localRotation = rot;
            _camera.farClipPlane = 2 * dist;
            _camera.orthographicSize = projectionConfig.Size.Y / 2;

            // Scaling
            //_camera.transform.localPosition *= 100;
            //_camera.farClipPlane *= 100;
            //_camera.orthographicSize *= 100;

            Debug.LogFormat("HostessViewCamera settings: position={0}, rotation={1}, farClipPlane={2}, orthographicSize={3}",
                _camera.transform.localPosition, _camera.transform.localRotation.eulerAngles, _camera.farClipPlane, _camera.orthographicSize);
        }

        void Start()
        {
            // Get and disable camera
            _camera = GetComponent<Camera>();
            _camera.enabled = false;
        }
    }
}