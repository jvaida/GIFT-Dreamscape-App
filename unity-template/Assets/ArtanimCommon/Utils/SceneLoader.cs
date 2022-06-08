using UnityEngine;
using UnityEngine.SceneManagement;

namespace Artanim {
    namespace Utils {
        [AddComponentMenu("AI/Utils/Scene Loader")]
        public class SceneLoader : MonoBehaviour {
            public string[] scenesList;
            public void loadScene( int sceneID ) {
                SceneManager.LoadScene(scenesList[sceneID]);
            }
        }
    }
}
