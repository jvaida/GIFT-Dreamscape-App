using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Artanim.HandAnimation.Procedural
{
	[Serializable]
	public struct AABB
	{
		public Vector3 Center;
		public Vector3 Extents;

		public AABB(float minX, float maxX, float minY, float maxY, float minZ, float maxZ)
		{
			Center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, (minZ + maxZ) * 0.5f);
			Extents = new Vector3(maxX - Center.x, maxY - Center.y, maxZ - Center.z);
		}

		public AABB(Vector3 pointA, Vector3 pointB, Vector3 pointC)
		{
			//Determine minimum and maximum values along all axes
			float min_x = MathUtils.Min3(pointA.x, pointB.x, pointC.x);
			float max_x = MathUtils.Max3(pointA.x, pointB.x, pointC.x);
			float min_y = MathUtils.Min3(pointA.y, pointB.y, pointC.y);
			float max_y = MathUtils.Max3(pointA.y, pointB.y, pointC.y);
			float min_z = MathUtils.Min3(pointA.z, pointB.z, pointC.z);
			float max_z = MathUtils.Max3(pointA.z, pointB.z, pointC.z);

			//Set the center and extents
			Center = new Vector3((min_x + max_x) * 0.5f, (min_y + max_y) * 0.5f, (min_z + max_z) * 0.5f);
			Extents = new Vector3(max_x - Center.x, max_y - Center.y, max_z - Center.z);
		}

		public AABB(List<Vector3> points)
		{
			//Initialize minimum values to the largest possible positive value
			//And the maximums to the largest negative value
			float min_x = float.MaxValue;
			float max_x = -float.MaxValue;
			float min_y = float.MaxValue;
			float max_y = -float.MaxValue;
			float min_z = float.MaxValue;
			float max_z = -float.MaxValue;

			for (int i = 0; i < points.Count; ++i)
			{
				if (points[i].x < min_x) min_x = points[i].x;
				if (points[i].x > max_x) max_x = points[i].x;
				if (points[i].y < min_y) min_y = points[i].y;
				if (points[i].y > max_y) max_y = points[i].y;
				if (points[i].z < min_z) min_z = points[i].z;
				if (points[i].z > max_z) max_z = points[i].z;
			}

			Center = new Vector3((min_x + max_x) * 0.5f, (min_y + max_y) * 0.5f, (min_z + max_z) * 0.5f);
			Extents = new Vector3(max_x - Center.x, max_y - Center.y, max_z - Center.z);
		}

		public AABB Merged(AABB other)
		{
			Vector3 min = Center - Extents;
			Vector3 max = Center + Extents;

			Vector3 min_other = other.Center - other.Extents;
			Vector3 max_other = other.Center + other.Extents;

			float min_x = min.x;
			float max_x = max.x;
			float min_y = min.y;
			float max_y = max.y;
			float min_z = min.z;
			float max_z = max.z;

			if (min_x > min_other.x) min_x = min_other.x;
			if (max_x < max_other.x) max_x = max_other.x;
			if (min_y > min_other.y) min_y = min_other.y;
			if (max_y < max_other.y) max_y = max_other.y;
			if (min_z > min_other.z) min_z = min_other.z;
			if (max_z < max_other.z) max_z = max_other.z;

			return new AABB(min_x, max_x, min_y, max_y, min_z, max_z);
		}

		// Lightly inflate an AABB's extents
		// Particularly useful for cases where a flat AABB may
		// be formed, such as for axis aligned triangles
		public void Inflate()
		{
			Extents += new Vector3(0.002f, 0.002f, 0.002f);
		}

		public bool InterSects(Plane plane)
		{
			// Compute the projection interval radius of b onto L(t) = b.c + t * p.n
			float r = Extents.x * Math.Abs(plane.normal.x) + Extents.y * Math.Abs(plane.normal.y) + Extents.z * Math.Abs(plane.normal.z);

			// Compute distance of box center from plane
			float s = Vector3.Dot(plane.normal, Center) + plane.distance;

			// Intersection occurs when distance s falls within [-r,+r] interval
			return Math.Abs(s) <= r;
		}

		public float SquareDistance(Vector3 point)
		{
			Vector3 local = point - Center;
			float dx = Math.Max(Math.Abs(local.x) - Extents.x, 0.0f);
			float dy = Math.Max(Math.Abs(local.y) - Extents.y, 0.0f);
			float dz = Math.Max(Math.Abs(local.z) - Extents.z, 0.0f);

			return dx * dx + dy * dy + dz * dz;
		}

	}

	[Serializable]
	public class AABBNode
	{
		public AABB Bounds;
		public int Left = -1;
		public int Right = -1;

		public bool IsLeaf()
		{
			//To avoid storing too much data unnecessarily, the Left index is also used as the Leaf index
			//That is, if the Right index == -1, it's not a Left/Right pair, but instead the Left value points at an index of Leaf data
			return (Left != -1 && Right == -1);
		}

		public void Draw()
		{
			Color previous = Gizmos.color;
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireCube(Bounds.Center, 2.0f * Bounds.Extents);
			Gizmos.color = previous;
		}
	}

	[Serializable]
	public class AABBTree
	{
		public AABBNode Root;
		public List<AABBNode> Nodes;

		public AABBTree()
		{
			Nodes = new List<AABBNode>();
		}

		public void ConstructFromMesh(ClosedMesh mesh)
		{
			int numTriangles = mesh.Triangles.Count;

			List<int> triangleIndices = new List<int>();
			for (int i = 0; i < numTriangles; ++i)
			{
				triangleIndices.Add(i);
			}

			int root_node_id = ConstructNode(mesh, triangleIndices, Nodes);
			Root = Nodes[root_node_id];
		}

		int ConstructNode(ClosedMesh mesh, List<int> triangle_indices, List<AABBNode> nodes)
		{
			List<Vertex> mesh_vertices = mesh.Vertices;

			//If we're left with a single index, we're at a leaf
			if (triangle_indices.Count == 1)
			{
				//Create a leaf node
				AABBNode leaf = new AABBNode
				{
					//TriangleIndex = triangle_indices[0]
					Left = triangle_indices[0]
				};

				var triangle = mesh.Triangles[triangle_indices[0]];
				Vector3 point_a = mesh_vertices[triangle.VertexIndices[0]].Position;
				Vector3 point_b = mesh_vertices[triangle.VertexIndices[1]].Position;
				Vector3 point_c = mesh_vertices[triangle.VertexIndices[2]].Position;

				leaf.Bounds = new AABB(point_a, point_b, point_c);
				leaf.Bounds.Inflate();

				nodes.Add(leaf);

				int node_idx = nodes.Count - 1;

				return node_idx;
			}
			else if (triangle_indices.Count > 1)
			{
				List<int> left_indices = new List<int>();
				List<int> right_indices = new List<int>();

				//Compute the bounding box for all points in the set of triangles
				//That is the lowest possible positive value above 0;
				float min_x = float.MaxValue;
				float max_x = -float.MaxValue;
				float min_y = float.MaxValue;
				float max_y = -float.MaxValue;
				float min_z = float.MaxValue;
				float max_z = -float.MaxValue;

				List<int> vertex_indices = new List<int>();

				for (int tri_idx = 0; tri_idx < triangle_indices.Count; ++tri_idx)
				{
					var triangle = mesh.Triangles[triangle_indices[tri_idx]];
					vertex_indices.Add(triangle.VertexIndices[0]);
					vertex_indices.Add(triangle.VertexIndices[1]);
					vertex_indices.Add(triangle.VertexIndices[2]);
				}

				vertex_indices = vertex_indices.Distinct().ToList();

				for (int i = 0; i < vertex_indices.Count; ++i)
				{
					Vector3 vec = mesh_vertices[vertex_indices[i]].Position;
					if (vec.x < min_x) min_x = vec.x;
					if (vec.x > max_x) max_x = vec.x;
					if (vec.y < min_y) min_y = vec.y;
					if (vec.y > max_y) max_y = vec.y;
					if (vec.z < min_z) min_z = vec.z;
					if (vec.z > max_z) max_z = vec.z;

				}

				AABB bounds = new AABB(min_x, max_x, min_y, max_y, min_z, max_z);

				if (triangle_indices.Count == 2)
				{
					left_indices.Add(triangle_indices[0]);
					right_indices.Add(triangle_indices[1]);
				}
				else
				{
					Vector3 extents = bounds.Extents;

					int axis;

					float split_value;

					if (extents.x >= extents.y && extents.x >= extents.z)
					{
						axis = 0;
						split_value = bounds.Center.x;
					}
					else if (extents.y >= extents.x && extents.y >= extents.z)
					{
						axis = 1;
						split_value = bounds.Center.y;
					}
					else
					{
						axis = 2;
						split_value = bounds.Center.z;
					}

					for (int i = 0; i < triangle_indices.Count; ++i)
					{
						Triangle triangle = mesh.Triangles[triangle_indices[i]];

						Vector3 veca = mesh.Vertices[triangle.VertexIndices[0]].Position;
						Vector3 vecb = mesh.Vertices[triangle.VertexIndices[1]].Position;
						Vector3 vecc = mesh.Vertices[triangle.VertexIndices[2]].Position;

						float center = (MathUtils.Min3(veca[axis], vecb[axis], vecc[axis]) + MathUtils.Max3(veca[axis], vecb[axis], vecc[axis])) / 2.0f;

						if (center < split_value)
						{
							left_indices.Add(triangle_indices[i]);
						}
						else
						{
							right_indices.Add(triangle_indices[i]);
						}
					}

					if (left_indices.Count == 0 || right_indices.Count == 0)
					{
						//It can happen on occasion that all elements fall on the same side
						//If so, just split them up.
						//That is semi-acceptable given that we're dealing with small sets for which this occurs 

						left_indices.Clear();
						right_indices.Clear();

						for (int i = 0; i < triangle_indices.Count; ++i)
						{
							if (i < triangle_indices.Count / 2)
							{
								left_indices.Add(triangle_indices[i]);
							}
							else
							{
								right_indices.Add(triangle_indices[i]);
							}
						}
					}
				}

				int left_id = ConstructNode(mesh, left_indices, nodes);
				int right_id = ConstructNode(mesh, right_indices, nodes);

				AABBNode node = new AABBNode()
				{
					Left = left_id,
					Right = right_id,
					Bounds = nodes[left_id].Bounds.Merged(nodes[right_id].Bounds)
				};

				nodes.Add(node);
				int idx = nodes.Count - 1;

				return idx;

			}

			Debug.LogError("[AABBTree] Trying to construct a node for no data! That shouldn't happen");
			return -1; //This should never happen
		}

		public void IntersectPlane(Plane plane, Vector3 position, float distance, List<int> interections)
		{
			IntersectNodePlane(Root, plane, position, distance, interections);
		}

		public void IntersectSphere(Vector3 position, float radius, List<int> intersection)
		{
			IntersectNodeSphere(Root, position, radius, intersection);
		}

		void IntersectNodePlane(AABBNode node, Plane plane, Vector3 position, float distance, List<int> intersected_leaves)
		{
			float nodeDistance = node.Bounds.SquareDistance(position);
			if (nodeDistance < (distance * distance) && node.Bounds.InterSects(plane))
			{
				if (node.IsLeaf())
				{
					intersected_leaves.Add(node.Left);
				}
				else
				{
					IntersectNodePlane(Nodes[node.Left], plane, position, distance, intersected_leaves);
					IntersectNodePlane(Nodes[node.Right], plane, position, distance, intersected_leaves);
				}
			}
		}

		void IntersectNodeSphere(AABBNode node, Vector3 position, float radius, List<int> intersectedLeaves)
		{
			float nodeDistance = node.Bounds.SquareDistance(position);
			if(nodeDistance > radius * radius)
			{
				return;
			}
			else
			{
				if (node.IsLeaf())
				{
					intersectedLeaves.Add(node.Left);
				}
				else
				{
					IntersectNodeSphere(Nodes[node.Left], position, radius, intersectedLeaves);
					IntersectNodeSphere(Nodes[node.Right], position, radius, intersectedLeaves);
				}
			}
		}

		#region Gizmo Drawing
		public void Draw(int level)
		{
			if (level < 0)
			{
				level = 0;
			}

			if (level == 0)
			{
				Root.Draw();
			}
			else
			{
				DrawNode(Root.Left, level - 1);
				DrawNode(Root.Right, level - 1);
			}
		}

		void DrawNode(int node_id, int current_level)
		{
			if (node_id == -1)
			{
				return;
			}

			if (current_level == 0)
			{
				Nodes[node_id].Draw();
			}
			else
			{
				DrawNode(Nodes[node_id].Left, current_level - 1);
				DrawNode(Nodes[node_id].Right, current_level - 1);
			}
		}
		#endregion
	}
}