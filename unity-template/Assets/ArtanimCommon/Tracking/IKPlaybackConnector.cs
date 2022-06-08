using Artanim.Location.Data;
using MsgPack.Serialization;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Artanim.Tracking
{
    public class IKPlaybackConnector : ITrackingConnector
    {
        public const string KEY_IK_PLAYER_IP = "ArtanimIKPlayerIp";

        public const string DEFAULT_IK_PLAYER_IP = "127.0.0.1:801";

        private const int SOCKET_READ_TIMEOUT = 30;

        public bool IsConnected
        {
            get
            {
                return UdpClient != null;
            }
        }

        public string Endpoint { get { return PlayerPrefs.GetString(KEY_IK_PLAYER_IP, DEFAULT_IK_PLAYER_IP); } }

        public string Version { get { return "0.1"; } }

        public ITrackingConnectorStats Stats { get { return PlaybackStats; } }

        class PlaybackStatistics : ITrackingConnectorStats
        {
            public uint FrameNumber { get; set; }
            public DateTime FrameCaptureTime { get; set; }
            public long FrameProcessTimestamp { get; set; }
            public float FrameCaptureLatency { get; set; }
            public float FrameProcessLatency { get; set; }
        }

        private PlaybackStatistics PlaybackStats = new PlaybackStatistics();

        private MessagePackSerializer<IKPlaybackFrame> Serializer = MessagePackSerializer.Get<IKPlaybackFrame>();
        private UdpClient UdpClient;
        private IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

        public void Connect()
        {
            IPEndPoint endPoint;
            var hostAndPort = PlayerPrefs.GetString(KEY_IK_PLAYER_IP, DEFAULT_IK_PLAYER_IP);
            if (TryParseIp(hostAndPort, out endPoint))
            {
                UdpClient = new UdpClient(endPoint);
                UdpClient.Client.ReceiveTimeout = SOCKET_READ_TIMEOUT;
                UdpClient.Client.ReceiveBufferSize = 2048;
            }
            else
            {
                Debug.LogErrorFormat("Failed to create connection to {0}", hostAndPort);
            }
        }

        public void Disconnect()
        {
            if(UdpClient != null)
            {
                UdpClient.Close();
                UdpClient = null;
            }
        }

        public bool UpdateRigidBodies()
        {
            try
            {
                var bytes = UdpClient.Receive(ref RemoteIpEndPoint);
                var playbackFrame = Serializer.UnpackSingleObject(bytes);

                if (playbackFrame != null)
                {
                    PlaybackStats.FrameCaptureTime = DateTime.UtcNow;
                    PlaybackStats.FrameNumber = playbackFrame.FrameNumber;

                    foreach (var rigidbody in playbackFrame.Rigidbodies)
                    {
                        var subject = TrackingController.Instance.GetOrCreateRigidBody(rigidbody.Name, applyRigidbodyConfig: true);

                        var sourcePosition = new Vector3(rigidbody.Position.X, rigidbody.Position.Y, rigidbody.Position.Z);
                        var sourceRotation = new Quaternion(rigidbody.Rotation.X, rigidbody.Rotation.Y, rigidbody.Rotation.Z, rigidbody.Rotation.W);

                        //Update rigidbody with Unity specific conversion
                        subject.UpdateTransform(
                            playbackFrame.FrameNumber,
                            Referential.TrackingPositionToUnity(sourcePosition),
                            Referential.TrackingRotationToUnity(sourceRotation.x, sourceRotation.y, sourceRotation.z, sourceRotation.w),
                            true,
                            sourcePosition: sourcePosition,
                            sourceRotation: sourceRotation);
                    }
                }
                return true;
            }
            catch
            {
                //Receive timeout
                return false;
            }
        }

        private bool TryParseIp(string value, out IPEndPoint result)
        {
            //replace "localhost" with ip
            value = value.ToLower().Trim().Replace("localhost", "127.0.0.1");

            Uri uri;
            IPAddress ipAddress;
            if (!Uri.TryCreate(string.Format("tcp://{0}", value), UriKind.Absolute, out uri) ||
                !IPAddress.TryParse(uri.Host, out ipAddress) || uri.Port < 0 || uri.Port > 65535)
            {
                result = default(IPEndPoint);
                return false;
            }

            result = new IPEndPoint(ipAddress, uri.Port);
            return true;
        }

    }
}