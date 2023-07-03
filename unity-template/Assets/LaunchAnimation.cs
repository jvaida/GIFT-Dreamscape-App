using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaunchAnimation : MonoBehaviour
{
    public Animator animator;
    // Start is called before the first frame update
    void Start()
    {
        animator.enabled = true;
        //animator.StartPlayback();
        //animator.Play(0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
