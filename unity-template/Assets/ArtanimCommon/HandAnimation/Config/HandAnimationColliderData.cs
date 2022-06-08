using UnityEngine;

namespace Artanim.HandAnimation.Procedural
{
	public class HandAnimationColliderData : ScriptableObject
	{
		public AABBTree AABBTree;
		public ClosedMesh Geometry;

		public void CreateFromMesh(Mesh mesh)
		{
			Geometry = new ClosedMesh(mesh);
			AABBTree = new AABBTree();
			AABBTree.ConstructFromMesh(Geometry);
		}
	}
}
