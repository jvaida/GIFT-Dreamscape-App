using Artanim.Tracking;
using Artanim.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Artanim.Monitoring.Utils
{
	public class TransformMetrics : TransformsMetricsBase
	{
		[Tooltip("Overrides the name logged for the transform data (optional)")]
		[SerializeField]
		string _nameOverride;

		Transform[] _transforms = new Transform[1];

		protected override void Initialize()
		{
			base.Initialize();

			_transforms[0] = transform;
        }

		protected override MetricsParams GetMetricsParams()
		{
			return new MetricsParams { InstanceName = _nameOverride == null ? string.Empty : _nameOverride, TemplateName = "Transform" };
		}

		protected override IList<Transform> GetTransforms()
        {
			return _transforms;
		}
	}
}