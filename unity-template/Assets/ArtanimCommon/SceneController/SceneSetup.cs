using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Artanim
{

	/// <summary>
	/// This behaviour holds scene scoped setup for the framework. Only one single SceneSetup is allowed to be present at a time.
	/// This behaviour can be derived to add additional custom functionality for the experience during scene loading.
	/// </summary>
	[AddComponentMenu("Artanim/Scene Setup")]
	public class SceneSetup : MonoBehaviour
	{
		[Tooltip("Scene scope camera override. The given camera template (prefab) will be used for this scene.")]
		public GameObject CameraTemplate;

		[Tooltip("Additional scenes to be loaded additive during the game scene load.")]
		public string[] AdditiveScenesToLoad;

		/// <summary>
		/// Override this method to add additional functionality during experience scene loading if needed.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerator SetupScene()
		{
			if (AdditiveScenesToLoad != null)
			{
				foreach (var sceneToLoad in AdditiveScenesToLoad)
				{
					yield return SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
				}
			}
		}
	}

}