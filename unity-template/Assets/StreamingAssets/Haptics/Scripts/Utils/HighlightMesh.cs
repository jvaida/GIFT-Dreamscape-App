using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Artanim.Haptics.Utils
{
	[RequireComponent(typeof(Renderer))]
	public class HighlightMesh : MonoBehaviour
	{
		public Color Color = Color.red;
		public float Duration = 0.2f;
		public Transform GroupRoot = null;
		public bool ManualControl = false;
		public bool RightHand = false;

		AvatarTrigger _avatarTrigger;

		string _materialColorName = "_Color";
		Material _material;
		Color _originalColor;

		public void Highlight(bool highlight = true)
		{
#if UNITY_2019_3_OR_NEWER
			if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
			{
				_materialColorName = "_BaseColor";
			}
#endif
			if (_material != null)
            {
				_material.SetColor(_materialColorName, highlight ? Color : _originalColor);
			}
		}

		void OnEnable()
        {
			if (!ManualControl)
            {
				if (_avatarTrigger == null)
				{
					_avatarTrigger = GetComponent<AvatarTrigger>();
				}
				if (RightHand)
                {
					_avatarTrigger.OnRightHandEnter.AddListener(OnHandEnter);
				}
				else
                {
					_avatarTrigger.OnHandEnter.AddListener(OnHandEnter);
				}
			}
		}

        void OnDisable()
        {
			if (_avatarTrigger != null)
            {
				_avatarTrigger.OnRightHandEnter.RemoveListener(OnHandEnter);
				_avatarTrigger.OnHandEnter.RemoveListener(OnHandEnter);
			}
		}

        void Awake()
		{
			// Can't do this at Start() because we need it ready right after instancing the script (ex: when in a prefab)
			_material = GetComponent<Renderer>().material;
			_originalColor = _material.GetColor(_materialColorName);
		}

		void OnHandEnter(AvatarController avatarController)
        {
			StartCoroutine(CrHighlight());

			if (GroupRoot)
            {
				foreach (var highlight in GroupRoot.GetComponentsInChildren<HighlightMesh>())
                {
					if (highlight != this)
                    {
						highlight.Highlight(false);
					}
				}
            }
		}

		IEnumerator CrHighlight()
		{
			Highlight();
			yield return new WaitForSecondsRealtime(Duration);
			if (Duration > 0)
            {
				Highlight(false);
			}
		}
	}
}