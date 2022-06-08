using UnityEngine;

[ExecuteInEditMode]
public class BlackSmearCorrectionEffect : MonoBehaviour
{
	public Shader shader;

	[HideInInspector]
	public float brightness = 0.0f;
	[HideInInspector]
	public float contrast = 0.0f;

	private Material material;
	
	void OnEnable()
	{
		if (!SystemInfo.supportsImageEffects)
		{
			enabled = false;
			return;
		}
		
		if (!shader || !shader.isSupported)
			enabled = false;
		else
		{
			material = new Material(shader);
			material.hideFlags = HideFlags.HideAndDontSave;
		}
	}
	
	void OnDisable()
	{
		if (material)
			DestroyImmediate(material);
	}

	void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (brightness == 0f && contrast == 0f)
		{
			Graphics.Blit(source, destination);
			return;
		}

		material.SetFloat("_Brightness", (brightness + 100f) * 0.01f);
		material.SetFloat("_Contrast", (contrast + 100f) * 0.01f);
		Graphics.Blit(source, destination, material);
	}
}
