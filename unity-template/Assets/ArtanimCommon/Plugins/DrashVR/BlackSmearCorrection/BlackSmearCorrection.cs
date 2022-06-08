using UnityEngine;

public class BlackSmearCorrection : MonoBehaviour
{
	[Range(-100f, 100f)]
	public float correctionContrastLevel = -2f;
	[Range(-100f, 100f)]
	public float correctionBrightnessLevel = 10f;
	public float smoothTime = 0.1f;
	public bool initiallyApplyCorrection = true;
	public KeyCode keyToToggleCorrection = KeyCode.F10;

	private BlackSmearCorrectionEffect[] blackSmearCorrectionEffects = null;
	private bool isApplyingCorrection = true;
	private float targetContrast = 0f;
	private float currentContrast = 0f;
	private float currentContrastVelocity = 0f;
	private float targetBrightness = 0f;
	private float currentBrightness = 0f;
	private float currentBrightnessVelocity = 0f;

	void Start()
	{
		blackSmearCorrectionEffects = GetComponentsInChildren<BlackSmearCorrectionEffect>(true);

		isApplyingCorrection = initiallyApplyCorrection;
		SetUpCorrection();
		currentContrast  = targetContrast;
		currentContrastVelocity = 0f;
		currentBrightness = targetBrightness;
		currentBrightnessVelocity = 0f;
	}

	void Update()
	{
		if(keyToToggleCorrection != KeyCode.None && Input.GetKeyDown(keyToToggleCorrection))
		{
			isApplyingCorrection = !isApplyingCorrection;
			SetUpCorrection();
		}

		if(currentContrast != targetContrast)
		{
			currentContrast = Mathf.SmoothDamp(currentContrast, targetContrast, ref currentContrastVelocity, smoothTime, 100f, Time.deltaTime);
			if(Approximately(currentContrast, targetContrast, 0.01f))
				currentContrast = targetContrast;
		}

		if(currentBrightness != targetBrightness)
		{
			currentBrightness = Mathf.SmoothDamp(currentBrightness, targetBrightness, ref currentBrightnessVelocity, smoothTime, 100f, Time.deltaTime);
			if(Approximately(currentBrightness, targetBrightness, 0.01f))
				currentBrightness = targetBrightness;
		}

		for(int i = 0; i < blackSmearCorrectionEffects.Length; i++)
		{
			blackSmearCorrectionEffects[i].contrast = currentContrast;
			blackSmearCorrectionEffects[i].brightness = currentBrightness;
			blackSmearCorrectionEffects[i].enabled = (currentContrast != 0f || currentBrightness != 0f);
		}
	}

	private void SetUpCorrection()
	{
		if(isApplyingCorrection)
		{
			targetContrast = correctionContrastLevel;
			targetBrightness = correctionBrightnessLevel;
		}
		else
		{
			targetContrast = 0f;
			targetBrightness = 0f;
		}
	}

	private bool Approximately(float value, float about, float range = 0.001f) 
	{
		return ((Mathf.Abs(value - about) < range));
	}
}
