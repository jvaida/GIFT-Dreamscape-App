using UnityEngine;

public class G2Chooser : MonoBehaviour
{
    public G2ChooserManager manager;   // Assign the manager in the Inspector
    public int index;                // Assign a unique index for each cube in the Inspector

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("OnTriggerEnter: " + index);
        manager.ActivateObject(index);
    }
}