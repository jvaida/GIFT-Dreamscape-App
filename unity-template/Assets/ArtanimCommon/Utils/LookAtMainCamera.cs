using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Artanim
{

public class LookAtMainCamera : MonoBehaviour
{
	void Update ()
	{
		transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
	}
}

}