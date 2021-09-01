using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Ikaroon.CSTT
{
	public class ScriptTemplateWindow : EditorWindow
	{
		const string HelpText =
			"Use #NAME# for the file and class name\n" +
			"Use #FOLDER# for using the folder as namespace for example. '/' will be replaced with '.'";

		[MenuItem("Tools/Ikaroon/Script Templates")]
		static void Init()
		{
			var window = GetWindow<ScriptTemplateWindow>();
			window.titleContent = new GUIContent("Script Templates");
			window.Show();
		}

		void OnGUI()
		{
			EditorGUILayout.HelpBox(new GUIContent(HelpText));

			foreach (var child in ScriptTemplateData.instance.ScriptTemplates.ToArray())
			{
				EditorGUILayout.BeginHorizontal();
				child.Expanded = EditorGUILayout.Foldout(child.Expanded, "Template: " + child.Name);
				if (GUILayout.Button("Clear", GUILayout.Width(50f)))
				{
					child.Content = string.Empty;
				}
				if (GUILayout.Button("X", GUILayout.Width(20f)))
				{
					ScriptTemplateData.instance.ScriptTemplates.Remove(child);
				}
				EditorGUILayout.EndHorizontal();

				if (child.Expanded)
				{
					child.Name = EditorGUILayout.TextField(child.Name);
					child.Content = EditorGUILayout.TextArea(child.Content);
				}

				EditorGUILayout.Space();
			}

			if (GUILayout.Button("Add Template"))
			{
				ScriptTemplateData.instance.ScriptTemplates.Add(new ScriptTemplate());
			}

			if (GUILayout.Button(new GUIContent("Apply")))
			{
				ScriptTemplateData.instance.Save();
				ScriptBuilder.GenerateScript();
			}
		}
	}
}