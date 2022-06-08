using System;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim.HandAnimation.Procedural
{
	[Serializable]
	public class Vertex
	{
		public Vector3 Position;
		public Vector3 Normal;

		public List<int> Triangles = new List<int>();
	}

	[Serializable]
	public class Edge : IEquatable<Edge>
	{
		public int IndexV1;
		public int IndexV2;
		public Vector3 Position;

		public Edge(int indexV1, int indexV2)
		{
			IndexV1 = indexV1;
			IndexV2 = indexV2;
		}

		//NOTE(Bart) We make the assumption here that we create edges
		//with vertex indices in a consistent order. That is, if we
		//have an edge (2, 7), we won't have another edge (7, 2)
		//We might want to guard against that? 
		public bool Equals(Edge other)
		{
			var edge = other as Edge;
			if(edge == null)
			{
				return false;
			}

			return (IndexV1 == edge.IndexV1 && IndexV2 == edge.IndexV2);
		}

		public override int GetHashCode()
		{
			int hash = 13;
			hash = (hash * 7) + IndexV1;
			hash = (hash * 7) + IndexV2;

			return hash;
		}
	}

	[Serializable]
	public class Triangle
	{
		public int[] VertexIndices = new int[3];
		public int[] EdgeIndices = new int[3];

		public Vector3 Normal;
	}

	[Serializable]
	public class ClosedMesh
	{
		public List<Vertex> Vertices;
		public List<int> TriangleIndices;
		public List<Triangle> Triangles;
		public List<Edge> Edges;
		public Vector3 CenterOfMass = Vector3.zero;
		public Bounds BoundingBox;

		public ClosedMesh(Mesh mesh)
		{
			int triangleCount = mesh.triangles.Length / 3;
			Triangles = new List<Triangle>(triangleCount);

			TriangleIndices = new List<int>();
			Vertices = new List<Vertex>();

			for (int i = 0; i < mesh.vertexCount; ++i)
			{
				Vertices.Add(new Vertex() { Position = mesh.vertices[i] });
			}

			TriangleIndices = new List<int>(mesh.triangles);

			for (int i = 0; i < Vertices.Count; i++)
			{
				CenterOfMass += Vertices[i].Position;
			}
			CenterOfMass = CenterOfMass / Vertices.Count;

			for (int i = 0; i < triangleCount; i++)
			{
				int idxa = TriangleIndices[i * 3];
				int idxb = TriangleIndices[i * 3 + 1];
				int idxc = TriangleIndices[i * 3 + 2];

				Vector3 va = Vertices[idxa].Position;
				Vector3 vb = Vertices[idxb].Position;
				Vector3 vc = Vertices[idxc].Position;

				Vector3 norm = Vector3.Cross((vb - va).normalized, (vc - va).normalized);

				Triangle newTriangle = new Triangle()
				{
					VertexIndices = new int[] { idxa, idxb, idxc},
					Normal = norm
				};

				Triangles.Add(newTriangle);

				int tri_idx = Triangles.Count - 1;
				Vertices[idxa].Triangles.Add(tri_idx);
				Vertices[idxb].Triangles.Add(tri_idx);
				Vertices[idxc].Triangles.Add(tri_idx);
			}

			Edges = new List<Edge>();
			for(int i = 0; i < Triangles.Count; ++i)
			{
				Triangle currentTriangle = Triangles[i];

				int va = currentTriangle.VertexIndices[0];
				int vb = currentTriangle.VertexIndices[1];
				int vc = currentTriangle.VertexIndices[2];

				Edge edgeAB = MakeEdge(va, Vertices[va], vb, Vertices[vb]);

				int edgeAB_idx = Edges.IndexOf(edgeAB);
				if(edgeAB_idx < 0)
				{
					Edges.Add(edgeAB);
					currentTriangle.EdgeIndices[0] = Edges.Count - 1;
				}
				else
				{
					currentTriangle.EdgeIndices[0] = edgeAB_idx;
				}

				Edge edgeBC = MakeEdge(vb, Vertices[vb], vc, Vertices[vc]);
				int edgeBC_idx = Edges.IndexOf(edgeBC);
				if (edgeBC_idx < 0)
				{
					Edges.Add(edgeBC);
					currentTriangle.EdgeIndices[1] = Edges.Count - 1;
				}
				else
				{
					currentTriangle.EdgeIndices[1] = edgeBC_idx;
				}

				Edge edgeCA = MakeEdge(vc, Vertices[vc], va, Vertices[va]);
				int edgeCA_idx = Edges.IndexOf(edgeCA);
				if (edgeCA_idx < 0)
				{
					Edges.Add(edgeCA);
					currentTriangle.EdgeIndices[2] = Edges.Count - 1;
				}
				else
				{
					currentTriangle.EdgeIndices[2] = edgeCA_idx;
				}
			}

			Vector3 min = Vertices[0].Position;
			Vector3 max = Vertices[0].Position;
			foreach(var vertex in Vertices)
			{
				//Min
				min.x = Mathf.Min(min.x, vertex.Position.x);
				min.y = Mathf.Min(min.y, vertex.Position.y);
				min.z = Mathf.Min(min.z, vertex.Position.z);
				//Max
				max.x = Mathf.Max(max.x, vertex.Position.x);
				max.y = Mathf.Max(max.y, vertex.Position.y);
				max.z = Mathf.Max(max.z, vertex.Position.z);
			}

			BoundingBox = new Bounds((min + max) / 2, max - min);

			foreach(var vertex in Vertices)
			{
				Vector3 normal = Vector3.zero;
				foreach(var triangle_index in vertex.Triangles)
				{
					normal += Triangles[triangle_index].Normal;
				}
				vertex.Normal = normal.normalized;
			}
		}

		float tolerance = 0.000001f;
		int SharedVertices(Triangle t1, Triangle t2, ref int[] sharedV)
		{
			int count = 0;
			for (int i = 0; i < t1.VertexIndices.Length; i++)
			{
				for (int j = 0; j < t2.VertexIndices.Length; j++)
				{
					if ((Vertices[t1.VertexIndices[i]].Position - Vertices[t2.VertexIndices[j]].Position).sqrMagnitude < tolerance)
					{
						sharedV[count] = t1.VertexIndices[i];
						count++;
						break;
					}
				}
			}
			return count;
		}

		Edge MakeEdge(int indexA, Vertex vertexA, int indexB, Vertex vertexB)
		{
			if(indexA < indexB)
			{
				return new Edge(indexA, indexB)
				{
					Position = (vertexA.Position + vertexB.Position) * 0.5f,
				};
			}
			else
			{
				return new Edge(indexB, indexA)
				{
					Position = (vertexA.Position + vertexB.Position) * 0.5f,
				};
			}
		}

		void MapDuplicateVertices(Vector3[] vertices, Dictionary<int, int> duplicateVertexMap, float Epsilon = 0.00001f)
		{
			HashSet<int> duplicates = new HashSet<int>();

			for(int i = 0; i < vertices.Length - 1; ++i)
			{
				for(int j = i + 1; j < vertices.Length; ++j)
				{
					if (!duplicates.Contains(i) && !duplicates.Contains(j))
					{
						if (i != j && Vector3.Distance(vertices[i], vertices[j]) < Epsilon)
						{
							duplicateVertexMap.Add(j, i);
							duplicates.Add(j);
						}
					}
				}
			}
		}

		public void DrawBoundingBox(Matrix4x4 mat)
		{
			Gizmos.color = Color.green;
			Gizmos.matrix = mat;

			Gizmos.DrawWireCube(BoundingBox.center, BoundingBox.size);

			Gizmos.matrix = Matrix4x4.identity;
			Gizmos.color = Color.white;

		}
	}
}