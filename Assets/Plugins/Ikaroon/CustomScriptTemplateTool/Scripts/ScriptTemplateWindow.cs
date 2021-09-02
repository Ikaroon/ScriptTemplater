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
			"Use #FOLDER# for using the folder as namespace for example. '/' will be replaced with '.'\n" +
			"Use #FILE# for the script file name with extension e.g.: Script.cs\n" +
			"Use #YEAR# for the year the script was created in\n" +
			"Use #DATE# for the date the script was created on\n" +
			"Use #COMPANY# for the company name that is set in the player settings\n" +
			"Use #UNITYUSER# for the username that's used at the moment\n" +
			"Use #SCRIPTTYPE# for automatically choosing between interface, class, and abstract class depending on the descriptors.";

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
					child.Name = EditorGUILayout.TextField(new GUIContent("Name"), child.Name);
					EditorGUILayout.LabelField(new GUIContent("Content"));
					child.Content = EditorGUILayout.TextArea(child.Content);
					child.InterfaceDescriptor = EditorGUILayout.TextField(new GUIContent("Interface Descriptor"), child.InterfaceDescriptor);
					child.AbstractDescriptor = EditorGUILayout.TextField(new GUIContent("Abstract Descriptor"), child.AbstractDescriptor);
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