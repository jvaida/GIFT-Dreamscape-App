using UnityEngine;
using System.Collections.Generic;

namespace Artanim.HandAnimation.Procedural
{
	public enum InteractionGeometryDescription
	{
		None,
		Real,
		Virtual
	}

	public enum InteractionGeometryType
	{
		Real = InteractionGeometryDescription.Real,
		Virtual = InteractionGeometryDescription.Virtual
	}

	[AddComponentMenu("Artanim/Hand Animation Collider")] 
	public class HandAnimationCollider : MonoBehaviour
	{
		public Mesh Mesh;
		public HandAnimationColliderData ColliderData;

		public InteractionGeometryType GeometryType = InteractionGeometryType.Virtual;
		[Range(0.0f, 1.0f)]
		public float ReachFactor = 1.0f;

		[HideInInspector]
		public List<HandAlignmentEdge> HandAlignmentEdges;

		private List<int> _Potentialintersections = new List<int>(50);
		private HashSet<Edge> _Visited = new HashSet<Edge>();
		private HashSet<Edge> _InRange = new HashSet<Edge>();

		void Awake()
		{

			if(ColliderData == null)
			{
				Debug.LogError("[HandAnimationCollider] No ColliderData was created or assigned for this collider!");
			}
		}

		private void OnEnable()
		{
			if (HandAnimationColliderManager.Instance != null)
			{
				HandAnimationColliderManager.Instance.RegisterCollider(this);
			}
		}

		private void OnDisable()
		{
			if (HandAnimationColliderManager.Instance != null)
			{
				HandAnimationColliderManager.Instance.RemoveCollider(this);
			}
		}

		private void OnDrawGizmosSelected()
		{
			if (ColliderData != null && ColliderData.Geometry != null)
			{
				ColliderData.Geometry.DrawBoundingBox(transform.localToWorldMatrix);

				if (Mesh != null && !Application.isPlaying)
				{
					Gizmos.matrix = transform.localToWorldMatrix;
					Gizmos.DrawWireMesh(Mesh);
				}
			}
		}

		/// <summary>
		/// Find a list of mesh/plane intersection points
		/// </summary>
		/// <param name="intersectionPoints">List of positions to which we'll add new intersection points</param>
		/// <param name="position">Plane position in world space</param>
		/// <param name="normal">Plane normal in world space</param>
		/// <param name="distance">Maximum distance from the position for which we'll find intersections</param>
		public void FindPlaneIntersectionPoints(List<IntersectionInfo> intersectionPoints, Vector3 position, Vector3 normal, float distance)
		{
			if(ColliderData == null)
			{
				return;
			}

			Vector3 localPosition = transform.InverseTransformPoint(position);
			Vector3 localNormal = transform.InverseTransformDirection(normal);
			distance = distance / transform.lossyScale.x; //We assume uniform scaling here. 

			Plane localPlane = new Plane(localNormal, localPosition);

			_Potentialintersections.Clear();
			ColliderData.AABBTree.IntersectPlane(localPlane, localPosition, distance, _Potentialintersections);

			if(_Potentialintersections.Count == 0)
			{
				return;
			}

			ComputePlaneIntersectionPoints(_Potentialintersections, localPosition, distance,  localPlane, intersectionPoints);
		}

		private void ComputePlaneIntersectionPoints(List<int> intersectedTriangles, Vector3 localPosition, float distance, Plane localPlane, List<IntersectionInfo> intersectionPoints)
		{
			_Visited.Clear();
			_InRange.Clear();

			foreach (var triangleIndex in intersectedTriangles)
			{
				Triangle currentTriangle = ColliderData.Geometry.Triangles[triangleIndex];

				int count = 0;
				for (int i = 0; i < 3; i++)
				{
					var currentEdge = ColliderData.Geometry.Edges[currentTriangle.EdgeIndices[i]];

					if (_Visited.Contains(currentEdge))
					{
						if (_InRange.Contains(currentEdge))
						{
							count++;
						}

						continue;
					}

					_Visited.Add(currentEdge);

					IntersectionInfo intersection;

					var va = ColliderData.Geometry.Vertices[currentEdge.IndexV1];
					var vb = ColliderData.Geometry.Vertices[currentEdge.IndexV2];
					if (IntersectionUtils.GetSegmentPlaneIntersection(va, vb, localPlane, out intersection))
					{
						if ((intersection.Position - localPosition).sqrMagnitude < distance * distance)
						{
							_InRange.Add(currentEdge);
							count++;
							intersectionPoints.Add(
								new IntersectionInfo(this, transform.TransformPoint(intersection.Position), transform.TransformDirection(intersection.Normal))
								);
						}
					}
				}

				if (count == 1) // only one intersection within reach
				{
					// look for point in triangle at the border of the reach circle
					var va = ColliderData.Geometry.Vertices[currentTriangle.VertexIndices[0]];
					var vb = ColliderData.Geometry.Vertices[currentTriangle.VertexIndices[0]];
					var vc = ColliderData.Geometry.Vertices[currentTriangle.VertexIndices[0]];
					IntersectionInfo intersection = IntersectionUtils.TriangleCircleIntersection(va, vb, vc, localPlane, distance, localPosition);
					intersectionPoints.Add(
						new IntersectionInfo(this, transform.TransformPoint(intersection.Position), transform.TransformDirection(intersection.Normal))
						);
				}
			}
		}
	}
}