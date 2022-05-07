using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;

namespace Ikaroon.ScriptTemplater
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

		[SerializeField]
		SerializedProperty m_template;

		public static ScriptTemplateWindow Edit(SerializedProperty templateProperty)
		{
			var nameProperty = templateProperty.FindPropertyRelative("m_name");
			ScriptTemplateWindow window = CreateInstance<ScriptTemplateWindow>();
			window.titleContent = new GUIContent($"Edit Template: {nameProperty.stringValue}");
			window.m_template = templateProperty;
			window.ShowUtility();
			return window;
		}

		void CreateGUI()
		{
			if (m_template == null)
			{
				Close();
				return;
			}

			VisualElement root = rootVisualElement;

			// Import UXML
			var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.ikaroon.scripttemplater/Editor/Data/UI/ScriptTemplateWindow.uxml");
			VisualElement uxml = visualTree.Instantiate();
			uxml.style.flexGrow = 1;
			root.Add(uxml);

			var nameProperty = m_template.FindPropertyRelative("m_name");
			var nameElement = uxml.Q<TextField>("name-field");
			nameElement.BindProperty(nameProperty);

			var contentProperty = m_template.FindPropertyRelative("m_content");
			var contentElement = uxml.Q<TextField>("content-field");
			contentElement.BindProperty(contentProperty);

			var interfaceProperty = m_template.FindPropertyRelative("m_interfaceDescriptor");
			var interfaceElement = uxml.Q<TextField>("interface-field");
			interfaceElement.BindProperty(interfaceProperty);

			var abstractProperty = m_template.FindPropertyRelative("m_abstractDescriptor");
			var abstractElement = uxml.Q<TextField>("abstract-field");
			abstractElement.BindProperty(abstractProperty);

			var removeButton = uxml.Q<Button>("import-button");
			removeButton.clicked += OnImportButtonClicked;
		}

		void OnImportButtonClicked()
		{
			var filePath = EditorUtility.OpenFilePanel("Import Script Template", Application.dataPath, "txt");
			if (string.IsNullOrEmpty(filePath))
				return;

			try 
			{
				var content = File.ReadAllText(filePath);

				var contentProperty = m_template.FindPropertyRelative("m_content");
				contentProperty.stringValue = content;
				m_template.serializedObject.ApplyModifiedProperties();
			}
			catch (IOException e)
			{
				// Import failed
				EditorUtility.DisplayDialog("Import failed", "The import failed with the following error: " + e.Message, "OK");
			}
		}
	}
}