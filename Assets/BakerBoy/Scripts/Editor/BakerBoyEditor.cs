using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BakerBoy))]
public class BakerBoyEditor : Editor
{
	public override void OnInspectorGUI ()
	{
		DrawDefaultInspector();

		if (GUILayout.Button("Bake"))
		{
			(target as BakerBoy).Bake();
		}
	}
}
