using System.Collections;
using System.Collections.Generic;
using Artanim;
using UnityEngine;

public class DSFSceneController : MonoBehaviour
{
    public void LoadGameSceneFadeToBlack( string sceneName )
    {
        LoadGameScene(sceneName, Artanim.Location.Messages.Transition.FadeBlack);
    }

    public void LoadGameSceneFadeToWhite(string sceneName)
    {
        LoadGameScene(sceneName, Artanim.Location.Messages.Transition.FadeWhite);
    }

    public void LoadGameScene(string sceneName)
    {
        LoadGameScene(sceneName, Artanim.Location.Messages.Transition.None);
    }

    private void LoadGameScene( string sceneName, Artanim.Location.Messages.Transition transition )
    {
        //make sure there is a GameController in the scene before attempting to load
        GameController _gameController = FindObjectOfType<GameController>();
        if ( _gameController != null )
        {
            _gameController.LoadGameScene(sceneName, transition); //Call the GameController function with specified transition type
        } else
        {
            Debug.LogError("Could not find a GameController in the scene while trying to switch scenes.");
        }
    }
}
