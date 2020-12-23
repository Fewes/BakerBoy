using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "New BakerBoy config", menuName = "BakerBoy/Config", order = 1)]
public class BakerBoyConfig : ScriptableObject
{
	public int			sampleCount				= 128;
	public bool			useHemisphere			= false;
	[Range(0, 1024)]
	public int			dilationCount			= 256;
	public Vector2Int	defaultBakeResolution	= new Vector2Int(1024, 1024);
	[Range(0, 1)]
	public float		depthBias				= 0.01f;

	[Header("Ambient Occlusion")]
	public bool			outputAmbientOcclusion	= true;
	[Range(0.001f, 8)]
	public float		occlusionBias			= 2f;
	[Range(1, 8)]
	public float		occlusionGain			= 1f;

	[Header("Bent Normal")]
	public bool			outputBentNormal		= true;

	[Header("Source Textures")]
	public bool			useSourceTextures		= true;
	public string		albedoMapName			= "_BaseMap";
	//public string		alphaTestEnabledName	= "_AlphaClip";
	//public string		alphaCutoffName			= "_Cutoff";
	public string		normalMapName			= "_BumpMap";
	//public string		normalMapScaleName		= "_BumpScale";
}