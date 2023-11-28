using UnityEngine;

public class ChooserManager : MonoBehaviour
{
    public GameObject[] objectsToControl;  // Assign the objects in the Inspector

    // This method is called by individual Choosers
    public void ActivateObject(int index)
    {
        Debug.Log("ActivateObject: " + index);
        // Turn off all objects
        foreach (var obj in objectsToControl)
        {
            obj.SetActive(false);
        }

        // Turn on the chosen one
        if (index >= 0 && index < objectsToControl.Length)
        {
            objectsToControl[index].SetActive(true);
        }
    }
}