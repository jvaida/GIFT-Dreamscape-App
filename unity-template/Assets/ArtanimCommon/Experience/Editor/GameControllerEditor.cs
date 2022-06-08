using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Artanim
{
    [CustomEditor(typeof(GameController))]
    public class GameControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            //Draw runtime infos
            var gameController = target as GameController;
            if(gameController)
            {
                var infos = new StringBuilder();

                //Players
                infos.AppendLine("Players:");
                if (gameController.RuntimePlayers.Count > 0)
                {
                    foreach(var player in gameController.RuntimePlayers)
                    {
                        infos.AppendFormat("\t{0}, isMain={1}\n", player.Player.ComponentId, player.IsMainPlayer);
                    }
                }
                else
                {
                    infos.AppendLine("No players registered.");
                }

                EditorGUILayout.HelpBox(infos.ToString(), MessageType.Info);
            }

        }

    }
}