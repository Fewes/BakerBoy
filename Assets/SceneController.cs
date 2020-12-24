using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SceneController : MonoBehaviour
{
	public Material[] skies;
	public Color[] albedoColors;

	readonly List<Material> sceneMaterials = new List<Material>();

	void Start ()
	{
		var renderers = FindObjectsOfType<MeshRenderer>();
		foreach (var renderer in renderers)
		{
			var material = Instantiate(renderer.sharedMaterial);
			renderer.material = material;
			sceneMaterials.Add(material);
		}

		SetSky(0);
	}

	void SetMaterialFloat (string parameter, float value)
	{
		foreach (var material in sceneMaterials)
			material.SetFloat(parameter, value);
	}

	void SetMaterialColor (string parameter, Color value)
	{
		foreach (var material in sceneMaterials)
			material.SetColor(parameter, value);
	}

	void SetMaterialKeyword (string parameter, bool state)
	{
		foreach (var material in sceneMaterials)
		{
			if (state)
				material.EnableKeyword(parameter);
			else
				material.DisableKeyword(parameter);
		}
	}

	public void SetReflectionOcclusionType (int value)
	{
		switch (value)
		{
			case 0:
				SetMaterialKeyword("_BENTNORMALMAP", false);
				SetMaterialFloat("_SimpleReflOcclusion", 0);
			break;
			case 1:
				SetMaterialKeyword("_BENTNORMALMAP", false);
				SetMaterialFloat("_SimpleReflOcclusion", 1);
			break;
			case 2:
				SetMaterialKeyword("_BENTNORMALMAP", true);
			break;
		}
	}

	public void SetSelfReflectionAmount (float value)
	{
		SetMaterialFloat("_SelfReflectionAmount", value);
	}

	public void SetSmoothness (float value)
	{
		SetMaterialFloat("_Smoothness", value);
	}

	public void SetMetalness (float value)
	{
		SetMaterialFloat("_Metallic", value);
	}

	public void SetSpecularAA (bool state)
	{
		SetMaterialKeyword("_SPECULAR_AA", state);
		SetMaterialFloat("_SpecularAA", state ? 1 : 0);
	}

	public void SetSky (int index)
	{
		RenderSettings.skybox = skies[index];
		DynamicGI.UpdateEnvironment();
	}

	public void SetAlbedoColor (int index)
	{
		SetMaterialColor("_BaseColor", albedoColors[index]);
	}
}
