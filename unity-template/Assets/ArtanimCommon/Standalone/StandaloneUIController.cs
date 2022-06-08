using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Artanim
{
    public class StandaloneUIController : MonoBehaviour
    {
        public Dropdown DropdownStanaloneScenes;

        #region Unity Events

        private void Start()
        {
            //Init scenes dropdown
            if (DropdownStanaloneScenes)
            {
                DropdownStanaloneScenes.gameObject.SetActive(true);
                DropdownStanaloneScenes.ClearOptions();

                //Load scenes
                DropdownStanaloneScenes.AddOptions(ConfigService.Instance.ExperienceConfig.StartScenes.Select(s => s.SceneName).ToList());
            }

            if (GameController.Instance)
                GameController.Instance.OnSceneLoadedInSession += Instance_OnSceneLoadedInSession;
        }

        #endregion

        #region Public Interface

        public void DoSelectScene()
        {
            if (DropdownStanaloneScenes && GameController.Instance)
            {
                GameController.Instance.LoadGameScene(DropdownStanaloneScenes.options[DropdownStanaloneScenes.value].text, Location.Messages.Transition.FadeBlack);
            }
        }

        #endregion

        #region Location Events

        /// Scene changed... update the scene dropdown
        private void Instance_OnSceneLoadedInSession(string[] sceneNames, bool sceneLoadTimedOut)
        {
            if (DropdownStanaloneScenes)
            {
                //Update dropdown selection
                var sceneOption = DropdownStanaloneScenes.options.FirstOrDefault(o => o.text == GameController.Instance.CurrentSession.CurrentScene);
                if (sceneOption != null)
                    DropdownStanaloneScenes.value = DropdownStanaloneScenes.options.IndexOf(sceneOption);
            }
        }

        #endregion

        #region Internals


        #endregion

    }
}