using Artanim.Location.Data;
using Artanim.Location.SharedData;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Artanim
{
    [RequireComponent(typeof(FollowOrbitCamera))]
    public class PlayerFocusSelection : MonoBehaviour
    {
        private int CurrentPlayerIndex;
        private Guid FocusedPlayerId;


        private void OnEnable()
        {
            if(GameController.Instance)
            {
                GameController.Instance.OnSessionPlayerLeft += Instance_OnSessionPlayerLeft;
                GameController.Instance.OnLeftSession += Instance_OnLeftSession;
            }
        }
        private void OnDisable()
        {
            if (GameController.Instance)
            {
                GameController.Instance.OnSessionPlayerLeft -= Instance_OnSessionPlayerLeft;
                GameController.Instance.OnLeftSession -= Instance_OnLeftSession;
            }
        }

        private void Instance_OnSessionPlayerLeft(Session session, Guid playerId)
        {
            if (playerId == FocusedPlayerId)
                DoShowSceneRoot();
        }

        private void Instance_OnLeftSession()
        {
            DoShowSceneRoot();
        }

        public void DoShowNextPlayer()
        {
            if (GameController.Instance.CurrentSession != null && GameController.Instance.RuntimePlayers.Count > 0)
            {
                if (CurrentPlayerIndex > GameController.Instance.RuntimePlayers.Count)
                    CurrentPlayerIndex = 0;

                CurrentPlayerIndex = (CurrentPlayerIndex + 1) % GameController.Instance.RuntimePlayers.Count;
                var player = GameController.Instance.RuntimePlayers[CurrentPlayerIndex];
                FocusPlayer(player);
            }
        }

        public void DoShowSceneRoot()
        {
            CurrentPlayerIndex = 0;
            FocusedPlayerId = Guid.Empty;
            GetComponent<FollowOrbitCamera>().Target = null;
        }

        public void FocusPlayer(RuntimePlayer player)
        {
            if(player != null)
            {
                GetComponent<FollowOrbitCamera>().Target = player.AvatarController.AvatarAnimator.transform;
                FocusedPlayerId = player.Player.ComponentId;
            }
            else
            {
                DoShowSceneRoot();
            }
        }

    }
}