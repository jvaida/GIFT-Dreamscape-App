using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerLevel1 : MonoBehaviour
{
    public GameObject[] itemsDeactivate;
    public GameObject[] itemsActive;
    public Animator animator;
    public string triggerName;
    public AudioSource source;
    public AudioClip buddyVoice;
    private void OnTriggerEnter(Collider other)
    {
        // animator.SetBool("G2 enter", true);
        // animator.Play(animationName);
    
        playAnimation();
        deactivateObjects();
        activateObjects();        
    }

    private void playAnimation(){
        if(animator != null){
            animator.SetTrigger(triggerName);
            playAudio();
            Debug.Log("Trigger Entered. Game object: "+ animator);
        } else {
            Debug.Log("No animator");
        }
    }

    private void playAudio(){
        if(source != null && !source.isPlaying){
            source.clip = buddyVoice;
            source.Play();
            Debug.Log("Buddy Audio was triggered & is playing " + buddyVoice);
        }
    }

    private void deactivateObjects(){
        if(itemsDeactivate.Length > 0){
            for (int i=0; i < itemsDeactivate.Length; i++){
                Debug.Log("Trigger Entered. Game object: "+ itemsDeactivate[i]);
                itemsDeactivate[i].SetActive(false);
            }
        }
    }

    private void activateObjects(){
        if(itemsActive.Length > 0){
            for (int j=0; j < itemsActive.Length; j++){
                itemsActive[j].SetActive(true);
            }
        }
    }
}
