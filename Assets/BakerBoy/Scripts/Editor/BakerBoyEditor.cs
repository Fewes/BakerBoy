using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BakerBoy))]
public class BakerBoyEditor : Editor
{
	#region Style
	Texture2D MakeTexture (Color color)
	{
		var tex = new Texture2D(1, 1);
		tex.SetPixel(0, 0, color);
		tex.Apply();
		return tex;
	}

	Texture2D _bakeTex;
	Texture2D bakeTex
	{
		get
		{
			if (!_bakeTex)
				_bakeTex = MakeTexture(new Color(0, 1, 0, 0.5f));
			return _bakeTex;
		}
	}

	Texture2D _halfBakeTex;
	Texture2D halfBakeTex
	{
		get
		{
			if (!_halfBakeTex)
				_halfBakeTex = MakeTexture(new Color(1, 1, 0, 0.5f));
			return _halfBakeTex;
		}
	}

	Texture2D _blockTex;
	Texture2D blockTex
	{
		get
		{
			if (!_blockTex)
				_blockTex = MakeTexture(new Color(1, 0, 1, 0.5f));
			return _blockTex;
		}
	}

	Texture2D _skipTex;
	Texture2D skipTex
	{
		get
		{
			if (!_skipTex)
				_skipTex = MakeTexture(new Color(1, 0, 0, 0.5f));
			return _skipTex;
		}
	}

	GUIStyle bakeStyle, halfBakeStyle, blockStyle, skipStyle;
	#endregion

	BakerBoy baker;

	SerializedProperty p_Config;
	SerializedProperty p_Items;

	void OnEnable ()
	{
		baker = target as BakerBoy;
		
		p_Config = serializedObject.FindProperty("config");
		p_Items = serializedObject.FindProperty("items");
	}

	void OnDisable ()
	{
		
	}

	GUIStyle GetStyle (BakerBoy.Item item)
	{
		if (item.bake)
		{
			if (item.occlude)
				return bakeStyle;
			else
				return halfBakeStyle;
		}
		else
		{
			if (item.occlude)
				return blockStyle;
			else
				return skipStyle;
		}
	}

	public override void OnInspectorGUI ()
	{
		if (bakeStyle == null)
		{
			bakeStyle = new GUIStyle(EditorStyles.helpBox);
			bakeStyle.normal.background = bakeTex;
			halfBakeStyle = new GUIStyle(EditorStyles.helpBox);
			halfBakeStyle.normal.background = halfBakeTex;
			blockStyle = new GUIStyle(EditorStyles.helpBox);
			blockStyle.normal.background = blockTex;
			skipStyle = new GUIStyle(EditorStyles.helpBox);
			skipStyle.normal.background = skipTex;
		}

		serializedObject.Update();

		EditorGUILayout.PropertyField(p_Config);

		EditorGUILayout.Space();

		GUILayout.BeginHorizontal();
		GUILayout.Label("Renderer", EditorStyles.boldLabel);
		GUILayout.Label("Bake", EditorStyles.boldLabel, GUILayout.Width(35));
		GUILayout.Label("Occlude", EditorStyles.boldLabel, GUILayout.Width(60));
		GUILayout.EndHorizontal();

		GUILayout.BeginVertical("box");

		int i = 0;
		foreach (var item in baker.items)
		{
			GUILayout.BeginHorizontal(GetStyle(item));
			//GUILayout.Label(item.renderer.name);
			if (GUILayout.Button(item.renderer ? item.renderer.name : "Missing renderer!"))
			{
				Selection.activeObject = item.renderer;
			}

			GUILayout.Space(20);
			EditorGUILayout.PropertyField(p_Items.GetArrayElementAtIndex(i).FindPropertyRelative("bake"), new GUIContent(""), GUILayout.Width(42));
			EditorGUILayout.PropertyField(p_Items.GetArrayElementAtIndex(i).FindPropertyRelative("occlude"), new GUIContent(""), GUILayout.Width(35));

			GUILayout.EndHorizontal();
			i++;
		}

		GUILayout.EndVertical();

		if (GUILayout.Button("Find Renderers"))
		{
			baker.FindItems();
		}

		EditorGUILayout.Space();

		if (GUILayout.Button("Bake"))
		{
			baker.Bake();
		}

		serializedObject.ApplyModifiedProperties();
	}
}
