using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LeapConfig
{
	public float ConfidenceThreshold = 0.5f;
	/** Obsolete values below **/
	public float CalibrationConfidenceThreshold = 0.8f;
	public float MaxHandOffset = 0.15f;
	public float RecalibrationThreshold = 0.05f;

#if UNITY_EDITOR
	[MenuItem("Artanim/Create Leap Config File")]
	public static void CreateConfigFile()
	{
		string json = EditorJsonUtility.ToJson(new LeapConfig(), true);
		string path = "Assets/StreamingAssets/leap_config.json";

		FileInfo file = new System.IO.FileInfo(path);
		file.Directory.Create(); // If the directory already exists, this method does nothing.

		File.WriteAllText(path, json);
	}
#endif

}
