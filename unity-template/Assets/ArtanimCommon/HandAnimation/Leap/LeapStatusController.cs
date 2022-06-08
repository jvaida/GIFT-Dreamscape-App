using Leap;
using Leap.Unity;
using System.Collections;
using UnityEngine;

namespace Artanim
{
	/// <summary>
	/// Component to monitor the device and service status of a Leap Motion sensor.
	/// The component is optionally able to restart the service if it's disconnected
	/// but the user running the experience should have permission to do so. 
	/// Todo: This component could ultimately be used to provide a hostess-visible status
	/// </summary>
	[RequireComponent(typeof(LeapXRServiceProvider))]
	public class LeapStatusController : ClientSideBehaviour
	{
		public enum eLeapStatus
		{
			None,
			DeviceDisconnected,
			DeviceOK,
			DeviceLost,
			DeviceFailed,
		}

		public enum eLeapServiceStatus
		{
			None,
			ServiceOK,
			ServiceDisconnected,
			ServiceRestarting
		}

		public bool AutoRestartService;

		private LeapXRServiceProvider _LeapXRServiceProvider;
		private Controller _LeapController;

		private eLeapStatus _LeapStatus = eLeapStatus.None;
		public eLeapStatus LeapStatus
		{
			get
			{
				return _LeapStatus;
			}
			set
			{
				if (_LeapStatus != value)
				{
					_LeapStatus = value;
					Debug.LogWarning("Leap Status Changed To " + value.ToString());
				}
			}
		}

		private eLeapServiceStatus _LeapServiceStatus = eLeapServiceStatus.None;
		public eLeapServiceStatus LeapServiceStatus
		{
			get
			{
				return _LeapServiceStatus;
			}
			set
			{
				if (_LeapServiceStatus != value)
				{
					_LeapServiceStatus = value;
					Debug.LogWarning("Leap Service Status Changed To " + value.ToString());
				}
			}
		}

		public void Start()
		{
			_LeapXRServiceProvider = GetComponent<LeapXRServiceProvider>();
			_LeapController = _LeapXRServiceProvider.GetLeapController();

			if (_LeapController != null)
			{
				_LeapController.Device += LeapController_Device;
				_LeapController.DeviceFailure += LeapController_DeviceFailure;
				_LeapController.DeviceLost += LeapController_DeviceLost;
			}
			else
			{
				Debug.LogError("Could not obtain a LeapController! Is the component set to execute after the LeapXRServiceProvider?");
			}
		}

		private void LeapController_DeviceLost(object sender, DeviceEventArgs e)
		{
			LeapStatus = eLeapStatus.DeviceLost;
		}

		private void LeapController_DeviceFailure(object sender, DeviceFailureEventArgs e)
		{
			LeapStatus = eLeapStatus.DeviceFailed;
		}

		private void LeapController_Device(object sender, DeviceEventArgs e)
		{
			LeapStatus = eLeapStatus.DeviceOK;
		}

		public void Update()
		{
			if(_LeapController == null)
			{
				return;
			}

			if (!_LeapXRServiceProvider.IsConnected() && LeapStatus == eLeapStatus.DeviceOK)
			{
				LeapStatus = eLeapStatus.DeviceDisconnected;
			}

			if (!_LeapController.IsServiceConnected)
			{
				if (AutoRestartService && LeapServiceStatus == eLeapServiceStatus.ServiceOK)
				{
					LeapServiceStatus = eLeapServiceStatus.ServiceRestarting;
					StartCoroutine(RestartLeapService());
				}
			}
			else
			{
				LeapServiceStatus = eLeapServiceStatus.ServiceOK;
			}
		}

		private IEnumerator RestartLeapService()
		{
			var stop_process = System.Diagnostics.Process.Start("net", "stop LeapService");
			while (!stop_process.HasExited)
			{
				yield return null;
			}

			if (stop_process.ExitCode != 0)
			{
				Debug.LogWarning("Could not stop LeapService. It may already have been stopped. Exit code = " + stop_process.ExitCode);
			}
			else
			{
				Debug.LogWarning("Stopped LeapService.");
			}

			var start_process = System.Diagnostics.Process.Start("net", "start LeapService");
			while (!start_process.HasExited)
			{
				yield return null;
			}
			if (start_process.ExitCode != 0)
			{
				Debug.LogError("Could not start LeapService! Does the current user have sufficient permissions? Exit code = " + start_process.ExitCode);
			}
			else
			{
				Debug.LogWarning("Restarted LeapService.");
			}
		}
	}
}