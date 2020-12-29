using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BakerBoy : MonoBehaviour
{
	#region Utilities
	static Texture2D RenderTextureToFile (RenderTexture rt, string filePath, bool isNormalMap = false, bool isHighQuality = false)
	{
		if (!rt)
			return null;

		RenderTexture.active = rt;
		var tex = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
		tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
		RenderTexture.active = null;
		var bytes = tex.EncodeToPNG();
		File.WriteAllBytes(filePath, bytes);
		if (Application.isPlaying)
			Destroy(tex);
		else
			DestroyImmediate(tex);

#if UNITY_EDITOR
		AssetDatabase.Refresh();
		var importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
		importer.textureType = isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
		importer.sRGBTexture = false;
		importer.textureCompression = isHighQuality ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.CompressedLQ;
		importer.SaveAndReimport();
		return AssetDatabase.LoadAssetAtPath(filePath, typeof(Texture2D)) as Texture2D;
#else
		return null;
#endif
	}

	static Vector3Int GetThreadGroupCount (int width, int height, int threadGroupSize)
	{
		return new Vector3Int(Mathf.CeilToInt((float)width / threadGroupSize), Mathf.CeilToInt((float)height / threadGroupSize), 1);
	}
	#endregion

	#region Classes & enums
	public enum DrawPass
	{
		Shadow,
		Gather,
		PackNormal
	}

	[System.Serializable]
	public class Item
	{
		public Renderer renderer;
		public bool bake;
		public bool occlude;

		public Item (Renderer renderer, bool bake, bool occlude)
		{
			this.renderer = renderer;
			this.bake = bake;
			this.occlude = occlude;
		}
	}

	public class RendererContainer
	{
		public Renderer renderer;
		public int submeshIndex;
		public bool bake;
		public bool occlude;
	}

	public class BakeContainer
	{
		static Material _internalMaterial;
		static Material internalMaterial
		{
			get
			{
				if (!_internalMaterial)
					_internalMaterial = new Material(Shader.Find("Hidden/BakerBoy"));
				return _internalMaterial;
			}
		}

		static ComputeShader _compute;
		static ComputeShader compute
		{
			get
			{
				if (!_compute)
					_compute = Resources.Load("BakerBoy") as ComputeShader;
				return _compute;
			}
		}

		const int SHADER_PASS_POSITION			= 0;
		const int SHADER_PASS_GATHER			= 1;
		const int SHADER_PASS_TRANSFORM_NORMALS = 2;

		public readonly List<RendererContainer> renderers = new List<RendererContainer>();

		public RenderTexture positionMap;
		public RenderTexture worldNormalMap;
		public RenderTexture occlusionMap;
		public RenderTexture bentNormalMap;

		public bool noOutput;

		Vector2Int resolution;
		BakerBoyConfig cachedConfig;

		public void Setup (BakerBoyConfig config, Material srcMaterial)
		{
			cachedConfig = config;

			if (noOutput)
				return;

			Texture2D srcMap = null;
			srcMap = srcMaterial.GetTexture(config.albedoMapName) as Texture2D;
			if (!srcMap)
				srcMap = srcMaterial.GetTexture(config.normalMapName) as Texture2D;
			resolution = config.defaultBakeResolution;
			if (srcMap)
				resolution = new Vector2Int(srcMap.width, srcMap.height);

			var desc = new RenderTextureDescriptor(resolution.x, resolution.y, RenderTextureFormat.ARGBFloat, 32) { enableRandomWrite = true, sRGB = false };
			positionMap		= new RenderTexture(desc);
			worldNormalMap	= new RenderTexture(desc);
			occlusionMap	= new RenderTexture(desc);
			bentNormalMap	= new RenderTexture(desc);
			positionMap.Create();
			worldNormalMap.Create();
			occlusionMap.Create();
			bentNormalMap.Create();

			positionMap.wrapMode	= TextureWrapMode.Repeat;
			worldNormalMap.wrapMode = TextureWrapMode.Repeat;
			occlusionMap.wrapMode	= TextureWrapMode.Clamp;
			bentNormalMap.wrapMode	= TextureWrapMode.Clamp;

			Graphics.SetRenderTarget(positionMap);
			GL.Clear(true, true, Color.clear);
			Graphics.SetRenderTarget(worldNormalMap);
			GL.Clear(true, true, Color.clear);
			Graphics.SetRenderTarget(occlusionMap);
			GL.Clear(true, true, Color.clear);
			Graphics.SetRenderTarget(bentNormalMap);
			GL.Clear(true, true, Color.clear);

			if (config.useUV2)
				internalMaterial.EnableKeyword("_USE_UV2");
			else
				internalMaterial.DisableKeyword("_USE_UV2");

			var cmd = CommandBufferPool.Get();
			cmd.SetRenderTarget(new RenderTargetIdentifier[] { positionMap.colorBuffer, worldNormalMap.colorBuffer }, positionMap.depthBuffer);
			var normalMap = srcMaterial.GetTexture(config.normalMapName);
			foreach (var rc in renderers)
			{
				if (!rc.bake)
					continue;

				if (normalMap && config.useSourceTextures)
				{
					cmd.SetGlobalTexture("_NormalMap", normalMap);
					cmd.SetGlobalFloat("_HasNormalMap", 1);
				}
				else
				{
					cmd.SetGlobalFloat("_HasNormalMap", 0);
				}
				cmd.DrawRenderer(rc.renderer, internalMaterial, rc.submeshIndex, SHADER_PASS_POSITION);
			}
			Graphics.ExecuteCommandBuffer(cmd);

			CommandBufferPool.Release(cmd);
		}

		public void Draw (CommandBuffer cmd, Material srcMaterial, DrawPass pass)
		{
			var tmp = Shader.PropertyToID("_WorldNormalMap");
			if (pass == DrawPass.PackNormal)
			{
				cmd.GetTemporaryRT(tmp, bentNormalMap.descriptor);
				cmd.Blit(bentNormalMap, tmp);
			}

			foreach (var rc in renderers)
			{
				switch (pass)
				{
					case DrawPass.Shadow:
						if (!rc.occlude)
							continue;

						// Find and use native shadow caster pass
						int shadowPass = srcMaterial.FindPass("ShadowCaster");
						if (shadowPass > -1)
						{
							cmd.DrawRenderer(rc.renderer, srcMaterial, rc.submeshIndex, shadowPass);
						}
					break;
					case DrawPass.Gather:
						if (!rc.bake)
							continue;

						cmd.SetRenderTarget(new RenderTargetIdentifier[] { occlusionMap.colorBuffer, bentNormalMap.colorBuffer }, occlusionMap.depthBuffer);
						cmd.ClearRenderTarget(true, false, Color.clear);
						cmd.SetGlobalTexture("_PositionMap", positionMap);
						cmd.SetGlobalTexture("_WorldNormalMap", worldNormalMap);
						cmd.SetGlobalFloat("_GatherAmount", 1f / cachedConfig.sampleCount);
						cmd.DrawRenderer(rc.renderer, internalMaterial, rc.submeshIndex, SHADER_PASS_GATHER);
					break;
					case DrawPass.PackNormal:
						if (!rc.bake)
							continue;

						cmd.SetRenderTarget(bentNormalMap);
						cmd.SetGlobalTexture("_WorldNormalMap", tmp);
						cmd.DrawRenderer(rc.renderer, internalMaterial, rc.submeshIndex, SHADER_PASS_TRANSFORM_NORMALS);
					break;
				}
			}

			if (pass == DrawPass.PackNormal)
			{
				cmd.ReleaseTemporaryRT(tmp);
			}
		}

		public void Draw (Material srcMaterial, DrawPass pass)
		{
			var cmd = CommandBufferPool.Get();
			Draw(cmd, srcMaterial, pass);
			Graphics.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}

		public void PostProcess ()
		{
			// Transform bent normal from world to tangent space and pack into [0 1] range
			Draw(null, DrawPass.PackNormal);

			// Apply occlusion bias/gain
			compute.SetTexture(0, "_Input", occlusionMap);
			compute.SetFloat("_OcclusionBias", cachedConfig.occlusionBias);
			compute.SetFloat("_OcclusionGain", (cachedConfig.useHemisphere ? 2 : 4) * cachedConfig.occlusionGain);
			var threadGroupCount = GetThreadGroupCount(occlusionMap.width, occlusionMap.height, 8);
			compute.Dispatch(0, threadGroupCount.x, threadGroupCount.y, threadGroupCount.z);

			// Dilate
			int dilationPasses = cachedConfig.dilationCount;
			if (dilationPasses > 0)
			{
				if (dilationPasses < 2)
					dilationPasses = 2;
				if (dilationPasses % 2 != 0)
					dilationPasses++;

				DilateTexture(ref occlusionMap, dilationPasses);
				DilateTexture(ref bentNormalMap, dilationPasses);
			}
		}

		static void DilationPass (RenderTexture input, RenderTexture output)
		{
			compute.SetTexture(1, "_Input", input);
			compute.SetTexture(1, "_Output", output);
			var threadGroupCount = GetThreadGroupCount(input.width, input.height, 8);
			compute.Dispatch(1, threadGroupCount.x, threadGroupCount.y, threadGroupCount.z);
		}

		static void SwapRTs (ref RenderTexture rt1, ref RenderTexture rt2)
		{
			var tmp = rt1;
			rt1 = rt2;
			rt2 = tmp;
		}

		static void DilateTexture (ref RenderTexture input, int passes)
		{
			var ping = input;
			var pong = RenderTexture.GetTemporary(ping.descriptor);
			pong.Create();
			for (int i = 0; i < passes; i++)
			{
				DilationPass(ping, pong);
				SwapRTs(ref ping, ref pong);
			}
			RenderTexture.ReleaseTemporary(pong);
		}

		public void Output (Material material)
		{
#if UNITY_EDITOR
			Object sourceAsset = material;
			if (cachedConfig.useSourceTextures)
			{
				if (material.GetTexture(cachedConfig.albedoMapName))
					sourceAsset = material.GetTexture(cachedConfig.albedoMapName);
				else if (material.GetTexture(cachedConfig.normalMapName))
					sourceAsset = material.GetTexture(cachedConfig.normalMapName);
			}

			var assetPath = AssetDatabase.GetAssetPath(sourceAsset);
			var texturePath = Path.GetDirectoryName(assetPath).Replace("\\", "/") + "/" + material.name;
			if (cachedConfig.combinedOutput)
			{
				compute.SetTexture(2, "_Input", occlusionMap);
				compute.SetTexture(2, "_Output", bentNormalMap);
				var threadGroupCount = GetThreadGroupCount(bentNormalMap.width, bentNormalMap.height, 8);
				compute.Dispatch(2, threadGroupCount.x, threadGroupCount.y, threadGroupCount.z);
				var result = RenderTextureToFile(bentNormalMap, texturePath + "_BentNormal.png", false, true);
				if (result && cachedConfig.useSourceTextures)
				{
					material.SetTexture(cachedConfig.bentNormalMapName, result);
				}
			}
			else
			{
				if (cachedConfig.outputAmbientOcclusion)
				{
					var result = RenderTextureToFile(occlusionMap, texturePath + "_Occlusion.png", false, true);
					if (result && cachedConfig.useSourceTextures)
					{
						material.SetTexture(cachedConfig.occlusionMapName, result);
					}
				}
				if (cachedConfig.outputBentNormal)
				{
					var result = RenderTextureToFile(bentNormalMap, texturePath + "_BentNormal.png", true, true);
					if (result && cachedConfig.useSourceTextures)
					{
						material.SetTexture(cachedConfig.bentNormalMapName, result);
					}
				}
			}
			AssetDatabase.Refresh();
#else
			Debug.Log("BakerBoy.BakeContainer.Output is an editor-only feature");
#endif
		}

		public void Dispose ()
		{
			if (positionMap)
			{
				positionMap.DiscardContents();
				positionMap.Release();
				positionMap = null;
			}
			if (worldNormalMap)
			{
				worldNormalMap.DiscardContents();
				worldNormalMap.Release();
				worldNormalMap = null;
			}
			if (occlusionMap)
			{
				occlusionMap.DiscardContents();
				occlusionMap.Release();
				occlusionMap = null;
			}
			if (bentNormalMap)
			{
				bentNormalMap.DiscardContents();
				bentNormalMap.Release();
				bentNormalMap = null;
			}
		}
	}
	#endregion

	#region Variables
	public readonly Dictionary<Material, BakeContainer> bakedMaterials = new Dictionary<Material, BakeContainer>();

	public BakerBoyConfig config = null;

	public List<Item> items = new List<Item>();

	Bounds m_SceneBounds;
	RenderTexture m_Shadowmap;
	#endregion

	#region Setup
	void OnValidate ()
	{
		if (!Application.isPlaying)
			FindItems();
	}

	public void FindItems ()
	{
		if (items == null)
			items = new List<Item>();

		var prevItems = items.GetRange(0, items.Count);
		items.Clear();

		var renderers = GetComponentsInChildren<Renderer>(true);

		foreach (var renderer in renderers)
		{
			if (!(renderer is MeshRenderer || renderer is SkinnedMeshRenderer))
				continue;

			bool bake = true;
			bool occlude = true;
			bool prevFound = false;
			foreach (var prevItem in prevItems)
			{
				if (prevItem.renderer == renderer)
				{
					bake = prevItem.bake;
					occlude = prevItem.occlude;
					prevFound = true;
					break;
				}
			}

			if (!prevFound)
			{
				if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
				{
					bake = false;
					occlude = false;
				}
				// Crude way of automatically skipping any renderer with _LOD1 or higher in the name
				// This could be improved substantially but proved sufficient at the time of implementation and is very simple
				else if (renderer.name.ToLower().Contains("_lod") && !renderer.name.ToLower().Contains("_lod0"))
				{
					bake = false;
					occlude = false;
				}
			}

			items.Add(new Item(renderer, bake, occlude));
		}
	}
	#endregion

	#region Baking
	bool SetupContainers ()
	{
		if (bakedMaterials.Count > 0)
		{
			foreach (var bake in bakedMaterials.Values)
			{
				bake.Dispose();
			}
		}

		bakedMaterials.Clear();

		var renderers = GetComponentsInChildren<Renderer>();

		bool foundRenderers = false;

		foreach (var item in items)
		{
			if (!item.bake && !item.occlude)
				continue;

			var renderer = item.renderer;

			if (!(renderer is MeshRenderer || renderer is SkinnedMeshRenderer))
				continue;

			if (!foundRenderers)
				m_SceneBounds = renderer.bounds;
			else
				m_SceneBounds.Encapsulate(renderer.bounds);

			foundRenderers = true;

			for (int i = 0; i < renderer.sharedMaterials.Length; i++)
			{
				Material material = renderer.sharedMaterials[i];

				if (!bakedMaterials.ContainsKey(material))
				{
					bakedMaterials.Add(material, new BakeContainer() { noOutput = !item.bake });
				}

				// noOutput flag for the entire material can only be set when initializing the container, and any renderer found that is set to bake will override the flag for this material
				if (item.bake)
					bakedMaterials[material].noOutput = false;

				bakedMaterials[material].renderers.Add(new RendererContainer() { renderer = renderer, submeshIndex = i, bake = item.bake, occlude = item.occlude });
			}
		}

		return foundRenderers;
	}

	void PreProcess ()
	{
		foreach (var bake in bakedMaterials)
		{
			bake.Value.Setup(config, bake.Key);
		}
	}

	void FitShadowCamera (Bounds sceneBounds, Vector3 direction, out Matrix4x4 view, out Matrix4x4 proj)
	{
		float sceneRadius = Mathf.Max(Mathf.Max(sceneBounds.extents.x, sceneBounds.extents.y), sceneBounds.extents.z);

		float nearPlane = 1;
		var cameraPosition = sceneBounds.center - direction * (sceneRadius + nearPlane);
		var cameraRotation = Quaternion.LookRotation(direction);

		view = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1)) * Matrix4x4.TRS(cameraPosition, cameraRotation, Vector3.one).inverse;
		proj = Matrix4x4.Ortho(-sceneRadius, sceneRadius, -sceneRadius, sceneRadius, nearPlane, sceneRadius * 2 + nearPlane);
	}

	void DrawShadowmap (Matrix4x4 view, Matrix4x4 proj)
	{
		var cmd = CommandBufferPool.Get();

		cmd.SetViewProjectionMatrices(view, proj);
		cmd.SetRenderTarget(m_Shadowmap);
		cmd.ClearRenderTarget(true, true, new Color(0, 0, 0, 1));

		foreach (var bake in bakedMaterials)
		{
			bake.Value.Draw(cmd, bake.Key, DrawPass.Shadow);
		}

		cmd.SetGlobalTexture("_ShadowMap", m_Shadowmap);

		// Calculate world-to-shadow matrix
		if (SystemInfo.usesReversedZBuffer) {
			proj.m20 = -proj.m20;
			proj.m21 = -proj.m21;
			proj.m22 = -proj.m22;
			proj.m23 = -proj.m23;
		}
		var scaleOffset = Matrix4x4.identity;
		scaleOffset.m00 = scaleOffset.m11 = scaleOffset.m22 = 0.5f;
		scaleOffset.m03 = scaleOffset.m13 = scaleOffset.m23 = 0.5f;
		var worldToShadow = scaleOffset * (proj * view);
		cmd.SetGlobalMatrix("_WorldToShadow", worldToShadow);
		
		Graphics.ExecuteCommandBuffer(cmd);
		CommandBufferPool.Release(cmd);
	}

	Vector3[] PointsOnSphere (int n)
	{
		var points = new Vector3[n];

		float inc = Mathf.PI * (3 - Mathf.Sqrt(5));
		float off = 2.0f / n;
		float x = 0;
		float y = 0;
		float z = 0;
		float r = 0;
		float phi = 0;

		for (var k = 0; k < n; k++)
		{
			y = k * off - 1 + (off /2);
			r = Mathf.Sqrt(1 - y * y);
			phi = k * inc;
			x = Mathf.Cos(phi) * r;
			z = Mathf.Sin(phi) * r;

			points[k] = new Vector3(x, y, z);
		}
		
		return points;
	}

	public void Bake ()
	{
		// Make sure we have everything we need to bake
		if (!config || !SetupContainers())
			return;

		// Set up bake containers
		PreProcess();

		// Initialize shadow map
		m_Shadowmap = new RenderTexture(4086, 4096, 32, RenderTextureFormat.Shadowmap);
		m_Shadowmap.Create();

		// Make sure we get the same result every bake
		Random.InitState(1234567890);

		// URP
		Shader.SetGlobalVector("_ShadowBias", new Vector4(0, 0, 0, 0));

		// BakerBoy
		Shader.SetGlobalFloat("_ShadowDepthBias", config.depthBias);

		// Sample loop
		bool canceled = false;
		Matrix4x4 view, proj;
		var cmd = CommandBufferPool.Get();
		var directions = PointsOnSphere(config.sampleCount);
		for (int i = 0; i < config.sampleCount; i++)
		{
#if UNITY_EDITOR
			//if (!Application.isPlaying)
			//{
			//	canceled = EditorUtility.DisplayCancelableProgressBar("Baking...", "Baking sample " + (i+1).ToString() + "/" + config.sampleCount, (i+0.5f) / config.sampleCount);
			//	if (canceled)
			//		break;
			//}
#endif

			cmd.Clear();

			// Random light direction
			Vector3 direction = directions[i];
			if (config.useHemisphere)
				direction.y = -Mathf.Abs(direction.y);
			cmd.SetGlobalVector("_LightDir", direction);

			// Set up virtual shadow camera
			FitShadowCamera(m_SceneBounds, direction, out view, out proj);

			// Render shadpwmap
			DrawShadowmap(view, proj);

			// Draw renderers
			foreach (var bake in bakedMaterials)
			{
				if (bake.Value.noOutput)
					continue;

				bake.Value.Draw(cmd, bake.Key, DrawPass.Gather);
			}

			// Execute the sample
			Graphics.ExecuteCommandBuffer(cmd);
		}
#if UNITY_EDITOR
		//EditorUtility.ClearProgressBar();
#endif

		CommandBufferPool.Release(cmd);

		// Release shadow map
		m_Shadowmap.DiscardContents();
		m_Shadowmap.Release();
		m_Shadowmap = null;

		if (!canceled)
		{
#if UNITY_EDITOR
			// Output files
			foreach (var bake in bakedMaterials)
			{
				if (bake.Value.noOutput)
					continue;

				bake.Value.PostProcess();
				bake.Value.Output(bake.Key);
			}
#endif
		}
		else
		{
			Debug.Log("Bake aborted by user");
		}

		// Discard RTs
		foreach (var bake in bakedMaterials)
		{
			if (bake.Value.noOutput)
					continue;

			bake.Value.Dispose();
		}

		// Re-seed RNG
		Random.InitState((int)System.DateTime.Now.Ticks);
	}
	#endregion
}
