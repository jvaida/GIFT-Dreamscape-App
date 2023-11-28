using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorBlinker : MonoBehaviour {

    public GameObject[] panels;
    private int index = 0;

    // Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void BlinkFloor()
    {
        StartCoroutine(BlinkFloorPanel());
    }

    private IEnumerator BlinkFloorPanel()
    {
        while (true)
        {
            panels[index].SetActive(!panels[index].activeSelf);
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void BlinkNextFloor()
    {
        panels[index].SetActive(true);
        index++;
        if (index == panels.Length)
            index = 0;
    }
}
