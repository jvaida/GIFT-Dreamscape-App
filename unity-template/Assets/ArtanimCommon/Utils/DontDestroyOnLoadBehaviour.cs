using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Artanim
{
	[AddComponentMenu("Artanim/Don't Destroy On Load")]
	public class DontDestroyOnLoadBehaviour : MonoBehaviour
	{
		public bool DestroyOnSessionTermination = true;
		public bool UniqueByName = false;

		void Awake()
		{
			if(UniqueByName)
			{
				//Search for other with the same name
				if(FindObjectsOfType<DontDestroyOnLoadBehaviour>().Count(o => o.name == gameObject.name) > 1)
				{
					//There's already one around... destroy this one
					Destroy(gameObject);
					return;
				}
			}
			
			if(transform.parent)
			{
				Debug.LogError("DontDestroyOnLoad won't work on game object because it's not on root: " + name);
            }
			DontDestroyOnLoad(gameObject);
		}

		void Start()
		{
			if(DestroyOnSessionTermination && GameController.Instance)
				GameController.Instance.OnLeftSession += Instance_OnLeftSession;
		}

        private void OnDestroy()
        {
            if(GameController.HasInstance)
			    GameController.Instance.OnLeftSession -= Instance_OnLeftSession;
        }

        private void Instance_OnLeftSession()
		{
			Destroy(gameObject);
		}
	}
}