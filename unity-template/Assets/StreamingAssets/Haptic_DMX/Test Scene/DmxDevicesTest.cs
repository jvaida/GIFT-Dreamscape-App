using Dreamscape;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DmxDevicesTest : MonoBehaviour
{
    [Range(0.1f, 5f)] public float DeviceTestTime = 1f;
    [Range(0f, 2f)] public float RampTime = 1f;

    private DMX_main _DMXMain;
    private DMX_main DMXMain
    {
        get
        {
            if (!_DMXMain)
                _DMXMain = FindObjectOfType<DMX_main>();
            return _DMXMain;
        }
    }
            
    void Start()
    {
        if(!DMXMain)
        {
            Debug.LogError("Failed to find DMX Main. Disabling test...");
            enabled = false;
            return;
        }

        if(DMXMain.dMX_Devices == null || DMXMain.dMX_Devices.Length == 0)
        {
            Debug.LogError("No devices setup in DMX Main. Disabling test...");
            enabled = false;
            return;
        }

        StartCoroutine(TestDevice(GetNextDevice(first: true)));
    }

    private IEnumerator TestDevice(DMX_device device)
    {
        Debug.LogFormat("Testing device: {0}", device.name);

        //Ramp up
        var rampStartTime = Time.realtimeSinceStartup;
        var time = 0f;
        var fromValue = 0f;
        var toValue = 1f;
        device.speed = fromValue;
        while (time != 1f)
        {
            time = Mathf.Clamp01((Time.realtimeSinceStartup - rampStartTime) / RampTime);
            device.speed = Mathf.Lerp(fromValue, toValue,  time);
            yield return null;
        }
        device.speed = toValue;

        //Leave running
        yield return new WaitForSecondsRealtime(DeviceTestTime);

        //Ramp down
        rampStartTime = Time.realtimeSinceStartup;
        time = 0f;
        fromValue = 1f;
        toValue = 0f;
        device.speed = fromValue;
        while (time != 1f)
        {
            time = Mathf.Clamp01((Time.realtimeSinceStartup - rampStartTime) / RampTime);
            device.speed = Mathf.Lerp(fromValue, toValue, time);
            yield return null;
        }
        device.speed = toValue;

        yield return new WaitForSecondsRealtime(DeviceTestTime);
        StartCoroutine(TestDevice(GetNextDevice()));
    }

    private int DeviceIndex = 0;
    private DMX_device GetNextDevice(bool first = false)
    {
        if (first)
        {
            return DMXMain.dMX_Devices[0];
        }
        else
        {
            DeviceIndex = (DeviceIndex + 1) % DMXMain.dMX_Devices.Length;
            return DMXMain.dMX_Devices[DeviceIndex];
        }
    }

}
