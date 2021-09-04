using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Ikaroon.CSTT
{
	public class ScriptTemplatesWindow : EditorWindow
	{
		[SerializeField]
		VisualElement m_templateList;

		[SerializeField]
		SerializedObject m_serializedObject;

		[SerializeField]
		SerializedProperty m_templateArray;

		[SerializeField]
		ScriptTemplateWindow m_templateEditWindow;

		[MenuItem("Edit/Script Template Settings", priority = 270)]
		static void Init()
		{
			ScriptTemplatesWindow window = GetWindow<ScriptTemplatesWindow>();
			window.titleContent = new GUIContent("Script Templates");
		}

		private void OnEnable()
		{
			Undo.undoRedoPerformed += Regenerate;
		}

		private void OnDisable()
		{
			Undo.undoRedoPerformed -= Regenerate;
		}

		void CreateGUI()
		{
			m_serializedObject = new SerializedObject(ScriptTemplateData.instance);

			VisualElement root = rootVisualElement;

			// Import UXML
			var visualTree = Resources.Load<VisualTreeAsset>("Ikaroon/UI/ScriptTemplatesWindow");
			VisualElement uxml = visualTree.Instantiate();
			uxml.style.flexGrow = 1;
			root.Add(uxml);

			var saveButton = uxml.Q<Button>("save-button");
			saveButton.clicked += SaveButtonClicked;

			var helpButton = uxml.Q<Button>("help-button");
			helpButton.clicked += HelpButtonClicked;

			var addButton = uxml.Q<Button>("add-button");
			addButton.clicked += AddButtonClicked;

			m_templateList = uxml.Q<VisualElement>("template-list");

			Regenerate();
		}

		void Regenerate()
		{
			m_serializedObject = new SerializedObject(ScriptTemplateData.instance);
			m_templateArray = m_serializedObject.FindProperty("m_scriptTemplates");

			m_templateList.Clear();
			var arraySize = m_templateArray.arraySize;
			for (int i = 0; i < arraySize; i++)
			{
				AddExistingTemplate(m_templateArray.GetArrayElementAtIndex(i));
			}
		}

		void SaveButtonClicked()
		{
			ScriptTemplateData.instance.Save();
			ScriptBuilder.GenerateScript();
		}

		void HelpButtonClicked()
		{
			Application.OpenURL("https://github.com/Ikaroon/CustomScriptTemplateTool");
		}

		void AddButtonClicked()
		{
			var group = Undo.GetCurrentGroup();
			Undo.RecordObject(ScriptTemplateData.instance, "Add Template");

			var index = m_templateArray.arraySize;
			m_templateArray.InsertArrayElementAtIndex(index);
			m_serializedObject.ApplyModifiedProperties();

			ScriptTemplateData.instance.ScriptTemplates[index] = new ScriptTemplate();
			var property = m_templateArray.GetArrayElementAtIndex(index);

			AddExistingTemplate(property);
			Undo.CollapseUndoOperations(group);
		}

		void AddExistingTemplate(SerializedProperty templateProperty)
		{
			var localProperty = templateProperty;
			var nameProperty = localProperty.FindPropertyRelative("m_name");

			var visualTree = Resources.Load<VisualTreeAsset>("Ikaroon/UI/ScriptTemplateItem");
			VisualElement uxml = visualTree.Instantiate();
			m_templateList.Add(uxml);

			var removeButton = uxml.Q<Button>("remove-button");
			removeButton.clicked += OnTemplateRemoveButtonClicked;

			void OnTemplateRemoveButtonClicked()
			{
				var group = Undo.GetCurrentGroup();
				if (m_templateEditWindow != null)
					m_templateEditWindow.Close();

				for (int i = 0; i < m_templateArray.arraySize; i++)
				{
					if (m_templateArray.GetArrayElementAtIndex(i).propertyPath == localProperty.propertyPath)
					{
						removeButton.clicked -= OnTemplateRemoveButtonClicked;
						m_templateList.Remove(uxml);

						Undo.RecordObject(ScriptTemplateData.instance, "Removed item");
						ScriptTemplateData.instance.ScriptTemplates.RemoveAt(i);
						return;
					}
				}
				Undo.CollapseUndoOperations(group);
			}

			var editButton = uxml.Q<Button>("edit-button");
			editButton.clicked += OnTemplateEditButtonClicked;

			void OnTemplateEditButtonClicked()
			{
				if (m_templateEditWindow != null)
					m_templateEditWindow.Close();

				m_templateEditWindow = ScriptTemplateWindow.Edit(localProperty);
			}

			var label = uxml.Q<Label>("template-name");
			label.BindProperty(nameProperty);
		}
	}
}