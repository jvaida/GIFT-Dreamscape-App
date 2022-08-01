using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

public class Haptic_Controller : MonoBehaviour
{
    public TextMeshProUGUI debugText;
    public bool operateDebug;
    [SerializeField]
    public GameObject floorHapticsObjectRocket;
    [SerializeField]
    public GameObject floorHapticsObjectPlatform;
    [SerializeField]
    public GameObject hapticFloors;
    [SerializeField]
    public GameObject fanHapticsObjectOn;
    [SerializeField]
    public GameObject fanHapticsObjectOff;

    // TODO: Check if using DS SDK or XR Quest build to switch haptic responses.


    private void Update()
    {
        if (operateDebug) debugText.SetText("Floor state:\t" + floorHapticsObjectPlatform.activeInHierarchy);
    }

    /// <summary>
    /// Invoked via Unity TL Signal Emitter
    /// </summary>
    /// <param name="amplitude">strength of haptic pulse</param>
    /// <param name="duration">duration of haptic pulse</param>
    public void ActivateHaptics(float amplitude, float duration)
    {
        Debug.Log("WOOP");
        /*if (!XRSettings.isDeviceActive) return; // in-case running in-editor

       XRBaseController[] xrBaseController = FindObjectsOfType<XRBaseController>();
       foreach (XRBaseController controller in xrBaseController)
           controller.SendHapticImpulse(amplitude, duration);         */

    }

    /// <summary>
    /// Invoked via Unity TL Signal Emitter
    /// </summary>
    /// <param name="amplitude">strength of haptic pulse</param>
    /// <param name="duration">duration of haptic pulse</param>
    public void ActivateHaptics(int duration)
    {
        if (!XRSettings.isDeviceActive) return; // in-case running in-editor

        XRBaseController[] xrBaseController = FindObjectsOfType<XRBaseController>();
        foreach (XRBaseController controller in xrBaseController)
            controller.SendHapticImpulse(1.0f, (float)duration);
    }

    IEnumerator PodHappticsFloor(int duration, GameObject floor)
    {
        floor.SetActive(true);
        yield return new WaitForSeconds(duration);
        floor.SetActive(false);
    }

    public void ActivatePodFloorsRocket(int duration)
    {
        StartCoroutine(PodHappticsFloor(duration, floorHapticsObjectRocket));
    }

    public void ActivatePodFloorsPod(int duration)
    {
        StartCoroutine(PodHappticsFloor(duration, floorHapticsObjectPlatform));
    }


    public void ActivatePodFans()
    {
        fanHapticsObjectOn.SetActive(true);
    }

    public void DeactivatePodFans()
    {
        fanHapticsObjectOn.SetActive(false);
    }

}