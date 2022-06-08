using Artanim.Location.Monitoring;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Artanim.Location.SharedData;
using System.Threading;
using Artanim.Location.Network;
using Artanim.Location.Data;

#if UNITY_2017_3_OR_NEWER
using UnityEngine.XR;
#else
using UnityEngine.VR;
using XRNode = UnityEngine.VR.VRNode;
using XRNodeState = UnityEngine.VR.VRNodeState;
using XRSettings = UnityEngine.VR.VRSettings;
using XRDevice = UnityEngine.VR.VRDevice;
#endif

using HmdMissing = Artanim.Location.Monitoring.OpTicketsTypes.Hmd.Missing;
using HmdUnplugged = Artanim.Location.Monitoring.OpTicketsTypes.Hmd.Unplugged;
using HmdOffHead = Artanim.Location.Monitoring.OpTicketsTypes.Hmd.OffHead;


namespace Artanim
{
    public class HMDMonitoring : MonoBehaviour
    {
        private OperationalTickets.IOpTicket HmdUnpluggedReport;
        private OperationalTickets.IOpTicket HmdOffHeadReport;

        private void Start()
        {
            //Check HMD availability
            if (NetworkInterface.Instance.IsTrueClient && !XRSettings.enabled)
            {
                OperationalTickets.Instance.OpenTicket(new HmdMissing() { ComponentId = SharedDataUtils.MySharedId.Guid });

                //No HMD, close app?
                if (DevelopmentMode.CurrentMode != EDevelopmentMode.Standalone && ConfigService.Instance.Config.Location.Client.ShutdownOnHMDMissing)
                {
                    Debug.LogErrorFormat("No HMD present. Shutting down client in {0}s.", ConfigService.Instance.Config.Location.Client.HMDMissingShutdownDelay);
                    Invoke("ShutdownClient", ConfigService.Instance.Config.Location.Client.HMDMissingShutdownDelay);
                }
            }
        }

        private void Update()
        {
            MonitorHmd();
        }

        private void MonitorHmd()
        {
            if (XRSettings.enabled)
            {
                //Device unplugged
                if (HmdUnpluggedReport != null && XRUtils.Instance.IsDevicePresent)
                {
                    //Close report
                    HmdUnpluggedReport.Close();
                    HmdUnpluggedReport = null;
                }
                else if (HmdUnpluggedReport == null && !XRUtils.Instance.IsDevicePresent)
                {
                    //Open report
                    HmdUnpluggedReport = OperationalTickets.Instance.OpenTicket(new HmdUnplugged { ComponentId = SharedDataUtils.MySharedId.Guid, HmdName = XRSettings.loadedDeviceName, });

                    //Close app?
                    if (DevelopmentMode.CurrentMode != EDevelopmentMode.Standalone && ConfigService.Instance.Config.Location.Client.ShutdownOnHMDDisconnect)
                    {
                        Debug.LogErrorFormat("HMD was disconnected. Shutting down client in {0}s.", ConfigService.Instance.Config.Location.Client.HMDDisconnectShutdownDelay);
                        Invoke("ShutdownClient", ConfigService.Instance.Config.Location.Client.HMDDisconnectShutdownDelay);
                    }
                }

                //User presence
                var userPresent = XRUtils.Instance.IsUserPresent;
                if (HmdOffHeadReport != null && userPresent)
                {
                    //Close report
                    HmdOffHeadReport.Close();
                    HmdOffHeadReport = null;
                }
                else if (HmdOffHeadReport == null && !userPresent)
                {
                    //Open report
                    HmdOffHeadReport = OperationalTickets.Instance.OpenTicket(new HmdOffHead { ComponentId = SharedDataUtils.MySharedId.Guid, HmdName = XRSettings.loadedDeviceName, });
                }

                //Limit fps when hmd is off head to avoid draining the battery too much
                if (!userPresent && ConfigService.Instance.ExperienceConfig.LimitFPSOnHMDOffHead)
                {
                    Thread.Sleep(1000 / 33);
                }
            }
        }

        private void ShutdownClient()
        {
            Debug.Log("Shutdown requested because of unplugged HMD. Shutting down...");
            Application.Quit();
        }

    }
}