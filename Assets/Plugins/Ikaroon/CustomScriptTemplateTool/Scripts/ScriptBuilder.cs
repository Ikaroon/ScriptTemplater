using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace Ikaroon.CSTT
{
	internal class DoCreateNewAsset : EndNameEditAction
	{
		ScriptTemplate m_template;

		public override void Action(int instanceId, string pathName, string resourceFile)
		{
			var assetPath = AssetDatabase.GenerateUniqueAssetPath(pathName);
			var textAsset = (TextAsset)EditorUtility.InstanceIDToObject(instanceId);
			AssetDatabase.CreateAsset(textAsset, assetPath);

			var fullPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
			var newPath = Path.ChangeExtension(fullPath, ".cs");
			var fileName = Path.GetFileName(newPath);

			var content = m_template.Content;

			var className = ScriptBuilder.GenerateValidClassName(Path.GetFileName(fullPath));

			content = content.Replace(ScriptBuilder.FormattedFileName, className);

			var folderSpace = Directory.GetParent(assetPath).ToString();
			folderSpace = folderSpace.Replace("/", ".");
			folderSpace = folderSpace.Replace("\\", ".");
			folderSpace = folderSpace.Replace(".", "_");
			folderSpace = ScriptBuilder.GenerateValidClassName(folderSpace);
			folderSpace = folderSpace.Replace("_", ".");
			content = content.Replace(ScriptBuilder.Folder, folderSpace);

			content = content.Replace(ScriptBuilder.Date, DateTime.Now.ToString("d"));
			content = content.Replace(ScriptBuilder.Year, DateTime.Now.ToString("yyyy"));
			content = content.Replace(ScriptBuilder.UnityUser, CloudProjectSettings.userName);
			content = content.Replace(ScriptBuilder.Company, PlayerSettings.companyName);
			content = content.Replace(ScriptBuilder.FileName, fileName);
			content = content.Replace(ScriptBuilder.ScriptType, m_template.GetScriptTypeFromName(fileName));

			File.WriteAllText(fullPath, content);
			File.Move(fullPath, newPath);
			File.Delete(fullPath + ".meta");
			AssetDatabase.Refresh();
		}

		public override void Cancelled(int instanceId, string pathName, string resourceFile)
		{
			Selection.activeObject = null;
		}

		public void SetTemplate(ScriptTemplate template)
		{
			m_template = template;
		}
	}

	public static class ScriptBuilder
	{
		internal const string FormattedFileName = "#NAME#";
		internal const string FileName = "#FILE#";
		internal const string Folder = "#FOLDER#";
		internal const string Date = "#DATE#";
		internal const string Year = "#YEAR#";
		internal const string UnityUser = "#UNITYUSER#";
		internal const string Company = "#COMPANY#";
		internal const string ScriptType = "#SCRIPTTYPE#";
		const string ClassNameChars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_";

		public static void GenerateFile(int template)
		{
			var createOperation = ScriptableObject.CreateInstance<DoCreateNewAsset>();
			var templateObject = ScriptTemplateData.instance.Templates[template];
			createOperation.SetTemplate(templateObject);
			var script = new TextAsset();
			var path = AssetDatabase.GetAssetPath(Selection.activeObject);
			path = Path.Combine(path, templateObject.Name);
			ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
				script.GetInstanceID(),
				createOperation,
				path,
				AssetPreview.GetMiniThumbnail(script), null);
		}

		public static bool ValidateSelection()
		{
			if (Selection.activeObject == null)
				return false;

			var path = AssetDatabase.GetAssetPath(Selection.activeObject);
			return Directory.Exists(path);
		}

		public static string GenerateValidClassName(string name)
		{
			var className = name.Replace('/', '-').Replace('\\', '-');

			var newString = new List<char>();
			foreach (var chr in className)
			{
				if (ClassNameChars.IndexOf(chr) != -1)
					newString.Add(chr);
			}
			className = string.Concat(newString);

			using (CodeDomProvider provider = CodeDomProvider.CreateProvider("C#"))
			{
				return provider.CreateValidIdentifier(className);
			}
		}

		public static void GenerateScript([CallerFilePath] string filePath = "")
		{
			var directory = Path.GetDirectoryName(filePath);
			var targetFile = Path.Combine(directory, "CSTTMenuHook.cs");

			if (!File.Exists(targetFile))
				File.Create(targetFile);

			using (CodeDomProvider provider = CodeDomProvider.CreateProvider("C#"))
			{
				using (var sw = new StreamWriter(targetFile))
				{
					IndentedTextWriter tw = new IndentedTextWriter(sw, "	");
					provider.GenerateCodeFromCompileUnit(GenerateClass(), tw, new CodeGeneratorOptions());
					tw.Close();
				}
			}

			AssetDatabase.Refresh();
		}

		static CodeCompileUnit GenerateClass()
		{
			CodeCompileUnit compileUnit = new CodeCompileUnit();

			CodeNamespace nameSpace = new CodeNamespace("Ikaroon.CSTT");
			compileUnit.Namespaces.Add(nameSpace);

			nameSpace.Imports.Add(new CodeNamespaceImport("UnityEditor"));

			CodeTypeDeclaration mainClass = new CodeTypeDeclaration("CSTTMenuHook");
			nameSpace.Types.Add(mainClass);

			var csScriptBuilderType = new CodeTypeReferenceExpression(typeof(ScriptBuilder).Name);
			var csMenuItemAttributeType = new CodeTypeReferenceExpression(typeof(MenuItem).Name);

			foreach (var template in ScriptTemplateData.instance.Templates)
			{
				var validator = new CodeMemberMethod();
				validator.Name = GenerateValidClassName(template.Name + "Validate");
				validator.ReturnType = new CodeTypeReference("static bool");

				var returnStatement = new CodeMethodReturnStatement();

				CodeMethodInvokeExpression validation = new CodeMethodInvokeExpression(
					csScriptBuilderType, nameof(ValidateSelection));
				returnStatement.Expression = validation;

				var validationAttribute = new CodeAttributeDeclaration(csMenuItemAttributeType.Type,
					new CodeAttributeArgument(new CodePrimitiveExpression("Assets/Create/Create " + template.Name)),
					new CodeAttributeArgument(new CodePrimitiveExpression(true)),
					new CodeAttributeArgument(new CodePrimitiveExpression(81)));

				validator.CustomAttributes.Add(validationAttribute);
				validator.Statements.Add(returnStatement);
				mainClass.Members.Add(validator);

				var creator = new CodeMemberMethod();
				creator.Name = GenerateValidClassName(template.Name);
				creator.ReturnType = new CodeTypeReference("static void");

				CodeMethodInvokeExpression generation = new CodeMethodInvokeExpression(
					csScriptBuilderType, nameof(GenerateFile),
					new CodePrimitiveExpression(ScriptTemplateData.instance.ScriptTemplates.IndexOf(template)));

				var attribute = new CodeAttributeDeclaration(csMenuItemAttributeType.Type,
					new CodeAttributeArgument(new CodePrimitiveExpression("Assets/Create/Create " + template.Name)),
					new CodeAttributeArgument(new CodePrimitiveExpression(false)),
					new CodeAttributeArgument(new CodePrimitiveExpression(81)));

				creator.CustomAttributes.Add(attribute);
				creator.Statements.Add(generation);
				mainClass.Members.Add(creator);
			}

			return compileUnit;
		}
	}
}
