using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Artanim.Monitoring.Utils;

namespace Artanim
{

	[RequireComponent(typeof(Camera))]
	public class RigidbodySelector : MonoBehaviour
	{
		public LayerMask LayerMask;

		private Camera Camera;
		private ISelectable SelectedItem;

		void Start()
		{
			Camera = GetComponent<Camera>();
		}

		void Update()
		{
#if IK_PROFILING
			IKProfiling.MarkRbSelectorStart();
#endif
			if (Camera)
			{
				RaycastHit hit;

				if (Physics.Raycast(Camera.ScreenPointToRay(Input.mousePosition), out hit, 100f, LayerMask))
				{
					var selectable = hit.collider.gameObject.GetComponent<ISelectable>();
					if (selectable != null)
					{
						//Deselect
						if (SelectedItem != null && SelectedItem != selectable)
							SelectedItem.Deselect();

						//Select
						SelectedItem = selectable;
						SelectedItem.Select();
					}
					else if (SelectedItem != null)
					{
						//Deselect
						SelectedItem.Deselect();
					}
				}
			}
#if IK_PROFILING
			IKProfiling.MarkRbSelectorEnd();
#endif
		}
	}

}