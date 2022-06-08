using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Artanim;

public class DisableInStandaloneMode : MonoBehaviour {

	// Use this for initialization
	void OnEnable () {
		if(DevelopmentMode.CurrentMode == EDevelopmentMode.Standalone)
        {
            gameObject.SetActive(false);
        }
	}
	
}
