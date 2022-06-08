using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
	[RequireComponent(typeof(Canvas))]
	public class AlwaysOnTopCanvas : MonoBehaviour
	{
		private const string DEFAULT_ON_TOP_UI_SHADER = "Artanim/UI Font Draw On Top";
		private const string DEFAULT_UNITY_UI_SHADER = "UI/Default";

		[Tooltip("Shader to use when rendering canvas on top. If left empty, the SDK 'Artanim/UI Font Draw On Top' shader is used.")]
		public Shader UIReplacementShader;

		[Tooltip("The Canvas shader to use when returning to the normal state. If left empty, the 'UI/Default' Unity shader is used.")]
		public Shader OriginalShader;

		[Tooltip("Checks if the Canvas is enabled and sets it on top.")]
		public bool CheckEnabled;

		[Tooltip("Checks if the CanvasGroup value is >0 and sets it on top.")]
		public bool CheckCanvasGroup;

		private Canvas Canvas;
		private CanvasGroup CanvasGroup;

		private bool _isOnTop;
		/// <summary>
		/// Sets the Canvas on top.
		/// </summary>
		public bool IsOnTop
		{
			get { return _isOnTop; }
			set
			{
				var shader = value ?
					UIReplacementShader ? UIReplacementShader : Shader.Find(DEFAULT_ON_TOP_UI_SHADER) :
					OriginalShader ? OriginalShader : Shader.Find(DEFAULT_UNITY_UI_SHADER);

				//Fallback do default UI shader if missing
				if(!shader)
					shader = Shader.Find(DEFAULT_UNITY_UI_SHADER);
				Canvas.GetDefaultCanvasMaterial().shader = shader;

				_isOnTop = value;
			}
		}

		private void Start()
		{
			Canvas = GetComponent<Canvas>();
			CanvasGroup = GetComponent<CanvasGroup>();
		}

		private void OnDisable()
		{
			IsOnTop = false;
		}

		private void Update()
		{
			if(CheckEnabled && Canvas.isActiveAndEnabled != IsOnTop)
				IsOnTop = Canvas.isActiveAndEnabled;

			if (CheckCanvasGroup && CanvasGroup && (CanvasGroup.alpha > 0) != IsOnTop)
				IsOnTop = CanvasGroup.alpha > 0;
		}


	}
}