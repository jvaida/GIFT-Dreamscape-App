using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Artanim
{
	public class MaterialReplaceDisplayController : AvatarDisplayController
	{
		[Tooltip("The avatars visual root used to show/hide the avatars body")]
		public GameObject AvatarVisualRoot;

		[Tooltip("List of materials to replace to show/hide head")]
		public MaterialReplacement[] ReplaceMaterials;

		[Tooltip("List of GameObjects to show/hide with head")]
		public GameObject[] ShowHeadObjects;

        public override void InitializePlayer(string initials)
        {

        }

        public override void ShowAvatar()
		{
			if (AvatarVisualRoot)
				AvatarVisualRoot.SetActive(true);
			else
				Debug.LogError("Failed to show avatar. No AvatarVisualRoot set.");
		}

		public override void HideAvatar()
		{
			if (AvatarVisualRoot)
				AvatarVisualRoot.SetActive(false);
			else
				Debug.LogError("Failed to hide avatar. No AvatarVisualRoot set.");
		}

		public override void ShowHead()
		{
			//Replace materials
			if(ReplaceMaterials != null && ReplaceMaterials.Length > 0)
			{
				foreach(var replaceMaterial in ReplaceMaterials)
				{
					if(replaceMaterial.Renderer && replaceMaterial.HeadShowMaterial)
					{
						var materials = replaceMaterial.Renderer.materials;
						materials[replaceMaterial.MaterialIndex] = replaceMaterial.HeadShowMaterial;
						replaceMaterial.Renderer.materials = materials;
					}
					else
					{
						Debug.LogError("Failed to hide head. No HeadBone set. List of ReplaceMaterials is not set properly. Make sure to set all renderers and materials needed.");
					}
				}
			}

			//Show GameObjects
			if(ShowHeadObjects != null && ShowHeadObjects.Length > 0)
			{
				foreach(var showObject in ShowHeadObjects)
				{
					showObject.SetActive(true);
				}
			}
		}

		public override void HideHead()
		{
			//Replace materials
			if (ReplaceMaterials != null && ReplaceMaterials.Length > 0)
			{
				foreach (var replaceMaterial in ReplaceMaterials)
				{
					if (replaceMaterial.Renderer && replaceMaterial.HeadHideMaterial)
					{
						var materials = replaceMaterial.Renderer.materials;
						materials[replaceMaterial.MaterialIndex] = replaceMaterial.HeadHideMaterial;
						replaceMaterial.Renderer.materials = materials;
					}
					else
					{
						Debug.LogError("Failed to hide head. No HeadBone set. List of ReplaceMaterials is not set properly. Make sure to set all renderers and materials needed.");
					}
				}
			}

			//Hide GameObjects
			if (ShowHeadObjects != null && ShowHeadObjects.Length > 0)
			{
				foreach (var showObject in ShowHeadObjects)
				{
					showObject.SetActive(false);
				}
			}
		}
	}

	[Serializable]
	public class MaterialReplacement
	{
		public Renderer Renderer;
		public int MaterialIndex;
		public Material HeadShowMaterial;
		public Material HeadHideMaterial;
	}
}