using Artanim.Tracking;
using Artanim.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Artanim.Monitoring.Utils
{
	public class TrackingRigidbodyTransformMetrics : TransformsMetricsBase
	{
		Transform[] _transforms = new Transform[1];
		string _instanceName = "";

		protected override void Initialize()
		{
			base.Initialize();

			_transforms[0] = transform;

			var rb = GetComponentInChildren<TrackingRigidbody>();
			if (rb == null)
			{
				rb = GetComponentInParent<TrackingRigidbody>();
			}
			if (rb != null)
			{
				if (!string.IsNullOrEmpty(rb.RigidbodyName))
				{
					_instanceName = rb.RigidbodyName.ToLowerInvariant();
				}
				else
				{
					Debug.LogErrorFormat("TrackingRigidbody {0} doesn't have a name for {1}", rb.name, name);
				}
			}
		}

		protected override MetricsParams GetMetricsParams()
		{
			return new MetricsParams { TemplateName = "TrackingRigidbodyTransform", InstanceName = _instanceName };
		}

		protected override IList<Transform> GetTransforms()
        {
			return _transforms;
		}
	}
}