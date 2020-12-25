using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "New BakerBoy config", menuName = "BakerBoy/Config", order = 1)]
public class BakerBoyConfig : ScriptableObject
{
	[Tooltip("The total number of occlusion samples")]
	public int			sampleCount				= 128;
	[Tooltip("Discard all occlusion samples that hit the ground")]
	public bool			useHemisphere			= false;
	[Range(0, 1024), Tooltip("How far pixels should be dilated in the output textures")]
	public int			dilationCount			= 256;
	[Tooltip("The bake resolution to use if baking without existing textures")]
	public Vector2Int	defaultBakeResolution	= new Vector2Int(1024, 1024);
	[Range(0, 1), Tooltip("The bias used when sampling the internal shadow map textures. A higher value results in less accurate occlusion, but also alleviates precision issues.")]
	public float		depthBias				= 0.01f;
	[Tooltip("Use the 2nd UV channel for the output textures")]
	public bool			useUV2					= false;
	[Tooltip("Output a combined texture with bent normal in RGB and ambient occlusion in A")]
	public bool			combinedOutput			= false;

	[Header("Ambient Occlusion")]
	public bool			outputAmbientOcclusion	= true;
	[Tooltip("Controls the contrast of the occlusion output")]
	[Range(0.001f, 8)]
	public float		occlusionBias			= 2f;
	[Tooltip("Controls the brightness of the occlusion output")]
	[Range(1, 8)]
	public float		occlusionGain			= 1f;

	[Header("Bent Normal")]
	public bool			outputBentNormal		= true;

	[Header("Source Textures")]
	[Tooltip("Attempt to use source textures from the original materials to enhance the bake")]
	public bool			useSourceTextures		= true;
	public string		albedoMapName			= "_BaseMap";
	//public string		alphaTestEnabledName	= "_AlphaClip";
	//public string		alphaCutoffName			= "_Cutoff";
	public string		normalMapName			= "_BumpMap";
	//public string		normalMapScaleName		= "_BumpScale";
	public string		occlusionMapName		= "_OcclusionMap";
	public string		bentNormalMapName		= "_BentNormalMap";
}