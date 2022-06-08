using Artanim.Location.Data;
using MsgPack.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

namespace Artanim
{
    public class IKPlayerRemote
    {
        #region Static Interface
        private static MessagePackSerializer CommandSerializer = MessagePackSerializer.Get<IKPlayerCommand>();
        private static UdpClient IKPlayerSocket = new UdpClient();

        public static string Host = "127.0.0.1";
        public static int Port = 802;

        public static void Play() { Play(Host, Port); }
        public static void Play(string host, int port)
        {
            SendCommand(new IKPlayerCommand { Command = IKPlayerCommand.ECommand.Play, }, host, port);
        }

        public static void Pause() { Pause(Host, Port); }
        public static void Pause(string host, int port)
        {
            SendCommand(new IKPlayerCommand { Command = IKPlayerCommand.ECommand.Pause, }, host, port);
        }

        public static void RestartCurrentFile() { RestartCurrentFile(Host, Port); }
        public static void RestartCurrentFile(string host, int port)
        {
            SendCommand(new IKPlayerCommand { Command = IKPlayerCommand.ECommand.RestartCurrent, }, host, port);
        }

        public static void NextRecording() { NextRecording(Host, Port); }
        public static void NextRecording(string host, int port)
        {
            SendCommand(new IKPlayerCommand { Command = IKPlayerCommand.ECommand.NextRecording, }, host, port);
        }

        public static void PrevRecording() { PrevRecording(Host, Port); }
        public static void PrevRecording(string host, int port)
        {
            SendCommand(new IKPlayerCommand { Command = IKPlayerCommand.ECommand.PrevRecording, }, host, port);
        }

        public static void NextFrame() { NextFrame(Host, Port); }
        public static void NextFrame(string host, int port)
        {
            SendCommand(new IKPlayerCommand { Command = IKPlayerCommand.ECommand.NextFrame, }, host, port);
        }

        public static void SetPlaybackSpeed(float speed) { SetPlaybackSpeed(Host, Port, speed); }
        public static void SetPlaybackSpeed(string host, int port, float speed)
        {
            SendCommand(new IKPlayerCommand { Command = IKPlayerCommand.ECommand.SetSpeed, Speed = speed, }, host, port);
        }

        public static void SetPlaybackMode(EPlaybackMode mode) { SetPlaybackMode(Host, Port, mode); }
        public static void SetPlaybackMode(string host, int port, EPlaybackMode mode)
        {
            SendCommand(new IKPlayerCommand { Command = IKPlayerCommand.ECommand.SetPlaybackMode, PlaybackMode = mode, }, host, port);
        }

        public static void SetRecording(string recordingFile, float startTime=0f) { SetRecording(Host, Port, recordingFile); }
        public static void SetRecording(string host, int port, string recordingFile, float startTime = 0f)
        {
            SendCommand(new IKPlayerCommand { Command = IKPlayerCommand.ECommand.SetRecording, RecordingFile = recordingFile, StartTime = startTime, },
                host, port);
        }


        public static void SendCommand(IKPlayerCommand command) { SendCommand(command, Host, Port); }
        public static void SendCommand(IKPlayerCommand command, string host, int port)
        {
            var bytes = CommandSerializer.PackSingleObject(command);
            IKPlayerSocket.Send(bytes, bytes.Length, host, port);
        }

        #endregion
    }
}