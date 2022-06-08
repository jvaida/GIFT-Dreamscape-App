using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Artanim
{

	public class UnityUtils
	{

		public static T InstantiatePrefab<T>(GameObject template, Transform root = null)
		{
			var instance = InstantiatePrefab(template, root);
			if(instance)
			{
				var script = instance.GetComponent<T>();
				if (script != null)
				{
					return script;
				}
				else
				{
					//Destroy instance
					GameObject.DestroyImmediate(instance);
				}
			}

			return default(T);
		}

		public static GameObject InstantiatePrefab(GameObject template, Transform root = null)
		{
			var instance = GameObject.Instantiate(template, Vector3.zero, Quaternion.identity, root) as GameObject;
			if (instance)
			{
				return instance;
			}

			return null;
		}

		public static T InstantiatePrefab<T>(string resource, Transform root)
		{
			return InstantiatePrefab<T>(Resources.Load<GameObject>(resource), root);
		}

		public static void RemoveAllChildren(Transform transform)
		{
			var children = new List<GameObject>();
			foreach (Transform child in transform)
				children.Add(child.gameObject);
			children.ForEach(child => UnityEngine.Object.Destroy(child));
		}

		public static float ClampAngle(float angle, float min, float max)
		{
			if (angle < -360F)
				angle += 360F;
			if (angle > 360F)
				angle -= 360F;
			return Mathf.Clamp(angle, min, max);
		}

		public static Vector3 ClampVector3(Vector3 value, Vector3 minValue, Vector3 maxValue)
		{
			return new Vector3()
			{
				x = Mathf.Clamp(value.x, minValue.x, maxValue.x),
				y = Mathf.Clamp(value.y, minValue.y, maxValue.y),
				z = Mathf.Clamp(value.z, minValue.z, maxValue.z),
			};
		}

		public static Bounds SceneBounds(List<GameObject> ignore = null)
		{
			var Bounds = new Bounds(Vector3.zero, Vector3.zero);
			foreach (var renderer in GameObject.FindObjectsOfType<Renderer>())
			{
				if (ignore.Contains(renderer.gameObject))
					continue;

				Bounds.Encapsulate(renderer.bounds);
			}

			return Bounds;
		}

		public static Bounds HierarchyBounds(Transform boundsRoot, bool OnlyVisible = true, bool excludeParticles = true)
		{
            Bounds bounds = new Bounds();
		
			if(boundsRoot)
			{
				var firstBound = true;
				foreach (var renderer in boundsRoot.GetComponentsInChildren<Renderer>())
				{
					if (excludeParticles && renderer.GetComponent<ParticleSystem>())
						continue;
					else if (OnlyVisible && !renderer.enabled)
						continue;
					else
					{
                        if(renderer is SkinnedMeshRenderer)
                        {
                            //Flip update when offscreen to make sure the renderer has recalculated its bounds!
                            (renderer as SkinnedMeshRenderer).updateWhenOffscreen = !(renderer as SkinnedMeshRenderer).updateWhenOffscreen;
                            (renderer as SkinnedMeshRenderer).updateWhenOffscreen = !(renderer as SkinnedMeshRenderer).updateWhenOffscreen;
                        }

                        if (firstBound)
                        {
                            bounds = renderer.bounds;
                            firstBound = false;
                        }
                        else
                        {
                            bounds.Encapsulate(renderer.bounds);
                        }
					}
				}
			}
		
			return bounds;
		}

		public static string GetChildPath(Transform parent, Transform child)
		{
			if(parent && child)
			{
				var path = new List<string>();
				var currentTransform = child;

				do
				{
					if(currentTransform)
					{
						//Append to path
						path.Add(currentTransform.name);
						currentTransform = currentTransform.parent;
					}

				} while (currentTransform != parent && currentTransform != null);

				//Render path
				if(currentTransform != null)
				{
					path.Reverse();

					return path.Aggregate((i, j) => i + "." + j);
				}
				else
				{
					//child is not child of parent!
					Debug.LogWarningFormat("{0} is not parent of {1}!", parent.name, child.name);
					return null;
				}

			}

			return null;
		}

		public static GameObject GetChildByPath(Transform root, string path)
		{
			if(root && !string.IsNullOrEmpty(path))
			{
				var pathElements = path.Split('.');

				var currentTransform = root;
				foreach(var element in pathElements)
				{
					currentTransform = currentTransform.Find(element);
					if(!currentTransform)
					{
						//Fail!
						//Debug.LogWarningFormat("Failed to find child={0} under root={1}", element, root.name);
						return null;
					}
				}

				return currentTransform.gameObject;
			}

			return null;
		}

        public static GameObject GetChildByName(string name, Transform parent)
        {
            for (var i = 0; i < parent.childCount; ++i)
            {
                var child = parent.GetChild(i);
                if (child.name == name)
                    return child.gameObject;
                else
                {
                    var found = GetChildByName(name, child);
                    if (found != null)
                        return found;
                }
            }

            return null;
        }

        public static Color ARGBStringToUnityColor(string colorString)
		{
			//#FF FF 00 00
			if(!string.IsNullOrEmpty(colorString) && colorString.StartsWith("#") && colorString.Length == 9)
			{
				try
				{
					var a = Convert.ToInt32(colorString.Substring(1, 2), 16);
					var r = Convert.ToInt32(colorString.Substring(3, 2), 16);
					var g = Convert.ToInt32(colorString.Substring(5, 2), 16);
					var b = Convert.ToInt32(colorString.Substring(7, 2), 16);

					return new Color((float)r / 255f, (float)g / 255f, (float)b / 255f, (float)a / 255f);
				}
				catch { }
			

			}
			return Color.magenta;
		}

		#region Transform utils

		public static Quaternion GetNormalized(Quaternion q)
		{
			//TODO: Change to 2018 built-in Quaternion.Normalize function when changing Unity version
			float f = 1f / Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
			return new Quaternion(q.x * f, q.y * f, q.z * f, q.w * f);
		}

		#endregion

		#region Matrix utils

		public static Quaternion ExtractMatrixRotation(Matrix4x4 matrix)
		{
			Vector3 forward;
			forward.x = matrix.m02;
			forward.y = matrix.m12;
			forward.z = matrix.m22;

			Vector3 upwards;
			upwards.x = matrix.m01;
			upwards.y = matrix.m11;
			upwards.z = matrix.m21;

			return Quaternion.LookRotation(forward, upwards);
		}

		public static Vector3 ExtractMatrixPosition(Matrix4x4 matrix)
		{
			Vector3 position;
			position.x = matrix.m03;
			position.y = matrix.m13;
			position.z = matrix.m23;
			return position;
		}

		public static Vector3 ExtractMatrixScale(Matrix4x4 matrix)
		{
			Vector3 scale;
			scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
			scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
			scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
			return scale;
		}

		#endregion

	}

}