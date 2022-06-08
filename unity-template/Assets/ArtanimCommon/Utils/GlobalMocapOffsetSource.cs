using UnityEngine;
using System.Collections;
using System.Linq;

namespace Artanim
{

	/// <summary>
	/// Register to server or client controller to globally offset the avatars to this transform.
	/// There should only be one GlobalMocapOffset active in the scene.
	/// </summary>
	[AddComponentMenu("Artanim/Global Mocap Offset Source")]
	public class GlobalMocapOffsetSource : MonoBehaviour
	{
		void Awake()
		{
			//Check for other mocap offsets
			if (GameObject.FindObjectsOfType<GlobalMocapOffsetSource>().Length > 1)
			{
				Debug.LogWarning("Found more than one GlobalMocapOffsetSource behaviour. There should only be one active! This can happen in a scene transition with load sequence set to LoadFirst.");
			}
		}

		void OnEnable()
		{
			if(GlobalMocapOffset.Instance)
			{
				GlobalMocapOffset.Instance.RegisterGlobalMocapOffset(transform);
			}
		}

		void OnDisable()
		{
			if (GlobalMocapOffset.Instance)
			{
				GlobalMocapOffset.Instance.UnRegisterGlobalMocapOffset(transform);
			}
		}

	}

}