using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamscape
{

	[ExecuteInEditMode]
	[RequireComponent(typeof(BoxCollider))]
	public class HapticAudio_floorPanel : MonoBehaviour
	{

        public List<Collider> playerFeet;

        // Use this for initialization
        void Start()
		{
            playerFeet = new List<Collider>();
        }

        public void OnTriggerEnter(Collider other)
        {
            if (other.tag == "FootClone")
            {
                playerFeet.Add(other);
                HapticAudio_main.Instance.UnMuteAudioChannel(this.gameObject.name);
            }
        }

        public void OnTriggerExit(Collider other)
        {
            if (other.tag == "FootClone")
            {
                playerFeet.Remove(other);
                if (playerFeet.Count == 0)
                    HapticAudio_main.Instance.MuteAudioChannel(this.gameObject.name);
            }
        }
    }

}