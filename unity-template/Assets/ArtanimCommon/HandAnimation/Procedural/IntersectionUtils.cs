using UnityEngine;

namespace Artanim.HandAnimation.Procedural
{
	public struct IntersectionInfo
	{
		public readonly HandAnimationCollider Geometry;
		public readonly Vector3 Position;
		public readonly Vector3 Normal;

		public IntersectionInfo(HandAnimationCollider geometry, Vector3 position, Vector3 normal)
		{
			Geometry = geometry;
			Position = position;
			Normal = normal;
		}

		public IntersectionInfo(Vector3 position, Vector3 normal)
		{
			Geometry = null;
			Position = position;
			Normal = normal;
		}

		public static readonly IntersectionInfo Empty = new IntersectionInfo();
	}

	public static class IntersectionUtils
	{
		const float Epsilon = 0.0000001f;

		public static float DistFromPlane(Vector3 point, Plane plane)
		{
			return Vector3.Dot(plane.normal, point) + plane.distance;
		}

		public static bool GetSegmentPlaneIntersection(Vertex ea, Vertex eb, Plane plane, out IntersectionInfo intersection)
		{
			if (ea == null && eb == null)
			{
				intersection = IntersectionInfo.Empty;
				return false;
			}

			float d1 = DistFromPlane(ea.Position, plane);
			float d2 = DistFromPlane(eb.Position, plane);

			// points on the same side of plane?
			if(Mathf.Sign(d1) == Mathf.Sign(d2)) 
			{
				intersection = IntersectionInfo.Empty;
				return false;
			}

			float t = d1 / (d1 - d2);
			Vector3 outSeg = ea.Position + t * (eb.Position - ea.Position);

			intersection = new IntersectionInfo(outSeg, (ea.Normal + eb.Normal).normalized);

			return true;
		}

		public static Vector3 IntersectionPoint(float r, Vector3 o, Vector3 l, Vector3 c)
		{
			float ac = l.sqrMagnitude;
			float bc = 2 * (Vector3.Dot(l, (o - c)));
			float cc = (o - c).sqrMagnitude - r * r;
			float d = (-bc + Mathf.Sqrt(bc * bc - 4 * ac * cc)) / (2 * ac);
			return o + d * l;
		}

		static Vector3[] triangleCircleIntersections = new Vector3[2]; //reuse to avoid CG alloc
		public static IntersectionInfo TriangleCircleIntersection(Vertex va, Vertex vb, Vertex vc, Plane plane, float reach, Vector3 pos)
		{
			triangleCircleIntersections[0] = Vector3.zero;
			triangleCircleIntersections[1] = Vector3.zero;

			int count = 0;

			IntersectionInfo intersection;
			if(IntersectionUtils.GetSegmentPlaneIntersection(va, vb, plane, out intersection))
			{
				triangleCircleIntersections[count++] = intersection.Position;
			}
			if (IntersectionUtils.GetSegmentPlaneIntersection(vb, vc, plane, out intersection))
			{
				triangleCircleIntersections[count++] = intersection.Position;
			}
			if (IntersectionUtils.GetSegmentPlaneIntersection(vc, va, plane, out intersection))
			{
				triangleCircleIntersections[count++] = intersection.Position;
			}

			var tri_normal = (va.Normal + vb.Normal + vc.Normal).normalized;

			if (count == 1)
			{
				return new IntersectionInfo(triangleCircleIntersections[0], tri_normal);
			}

			float sqrReach = reach * reach;
			Vector3 intersectionPoint = ((triangleCircleIntersections[0] - pos).sqrMagnitude < sqrReach) ?
				IntersectionUtils.IntersectionPoint(reach, triangleCircleIntersections[0], triangleCircleIntersections[1] - triangleCircleIntersections[0], pos) :
				IntersectionUtils.IntersectionPoint(reach, triangleCircleIntersections[1], triangleCircleIntersections[0] - triangleCircleIntersections[1], pos);

			return new IntersectionInfo(intersectionPoint, tri_normal);
		}
	}

}

