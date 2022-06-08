using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Artanim;

namespace Dreamscape
{
	public class HapticFootColliderMirror : MonoBehaviour
	{
		public GameObject[] footClones;
		public GameObject[] avatarFeet;
		public GameObject footClonePrefab;
		public Transform hapticFloorRoot;
		//private Transform globalMocapOffset;
		private bool initialized;

		private void OnEnable()
		{
			if (GameController.Instance)
			{
				GameController.Instance.OnSessionStarted += Instance_OnSessionStarted;
				GameController.Instance.OnLeftSession += Instance_OnLeftSession;
			}
		}

		private void OnDisable()
		{
			if (GameController.Instance)
			{
				GameController.Instance.OnSessionStarted -= Instance_OnSessionStarted;
				GameController.Instance.OnLeftSession -= Instance_OnLeftSession;
			}
		}

		private void Instance_OnLeftSession()
		{
			DisableFootColliders();
		}

		private void Instance_OnSessionStarted()
		{
			CreateFootMirrorColliders();
		}

		public void DisableFootColliders()
		{
			initialized = false;
            foreach (GameObject go in footClones)
                Destroy(go);

		}

		private void CreateFootMirrorColliders()
		{
            DisableFootColliders(); //TODO - not sure why but this gets called twice - at least in standalone mode

			avatarFeet = GameObject.FindGameObjectsWithTag("PlayerFeet");
			Debug.Log("found " + avatarFeet.Length + " avatar feet");
			footClones = new GameObject[avatarFeet.Length];
			for (int i = 0; i < avatarFeet.Length; i++)
			{
                Debug.Log(avatarFeet[i].name);
				Debug.Log("instantiating foot clone");
				footClones[i] = Instantiate(footClonePrefab, this.transform);
				footClones[i].transform.SetPositionAndRotation(hapticFloorRoot.position, hapticFloorRoot.rotation);
			}
			initialized = true;
		}

		// Update is called once per frame
		void Update()
		{
			if (initialized)
			{

				for (int i = 0; i < avatarFeet.Length; i++)
				{
					if (avatarFeet[i] != null)
					{
						
						RuntimePlayer rp;
						if (DevelopmentMode.CurrentMode == EDevelopmentMode.Standalone)
							rp = GameController.Instance.RuntimePlayers[0];
						else
							rp = GameController.Instance.GetPlayerByPlayerId(avatarFeet[i].GetComponentInParent<AvatarController>().PlayerId);
						if (rp != null)
						{
                            footClones[i].gameObject.SetActive(true);
                            footClones[i].transform.position = rp.AvatarOffset.transform.InverseTransformPoint(avatarFeet[i].transform.position);
                            
						}
                        else
                        {
                            footClones[i].gameObject.SetActive(false);
                            //Debug.Log("No runtime player for avatar controller!");
                        }
                    }
					else
					{
						footClones[i].gameObject.SetActive(false);
					}
				}
			}
		}
	}
}