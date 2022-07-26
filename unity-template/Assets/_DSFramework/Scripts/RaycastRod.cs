using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastRod : MonoBehaviour
{
    bool hitting;
    // Start is called before the first frame update
    void Start()
    {
        hitting = false;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit))
        {
            Debug.Log("hit in change layer");
            if (!hitting)
            {
                MaterialSwitch script = hit.collider.gameObject.GetComponent<MaterialSwitch>();
                if (script)
                {
                    Debug.Log("has desired script");
                    hit.transform.SendMessage("Change");
                }
                hitting = true;
            }

        }
        else
        {
            hitting = false;
        }
    }
}
