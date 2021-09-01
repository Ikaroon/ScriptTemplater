﻿using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace Ikaroon.CSTT
{
	internal class DoCreateNewAsset : EndNameEditAction
	{
		public override void Action(int instanceId, string pathName, string resourceFile)
		{
			var assetPath = AssetDatabase.GenerateUniqueAssetPath(pathName);
			var textAsset = (TextAsset)EditorUtility.InstanceIDToObject(instanceId);
			AssetDatabase.CreateAsset(textAsset, assetPath);

			var fullPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);

			var content = textAsset.text;

			var className = ScriptBuilder.GenerateValidClassName(Path.GetFileName(fullPath));

			content = content.Replace(ScriptBuilder.FileName, className);
			var folderSpace = assetPath.Replace("/", ".");
			folderSpace = folderSpace.Replace("\\", ".");
			folderSpace = folderSpace.Replace(".", "_");
			folderSpace = ScriptBuilder.GenerateValidClassName(folderSpace);
			folderSpace = folderSpace.Replace("_", ".");
			content = content.Replace(ScriptBuilder.Folder, folderSpace);

			File.WriteAllText(fullPath, content);
			var newPath = Path.ChangeExtension(fullPath, ".cs");
			File.Move(fullPath, newPath);
			File.Delete(fullPath + ".meta");
			AssetDatabase.Refresh();
		}

		public override void Cancelled(int instanceId, string pathName, string resourceFile)
		{
			Selection.activeObject = null;
		}
	}

	public static class ScriptBuilder
	{
		internal const string FileName = "#NAME#";
		internal const string Folder = "#FOLDER#";
		const string ClassNameChars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_";

		public static void GenerateFile(string temporaryName, string content)
		{
			var text = content;
			var script = new TextAsset(text);
			var path = AssetDatabase.GetAssetPath(Selection.activeObject);
			path = Path.Combine(path, temporaryName);
			ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
				script.GetInstanceID(),
				ScriptableObject.CreateInstance<DoCreateNewAsset>(),
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
					new CodeAttributeArgument(new CodePrimitiveExpression("Assets/Create/" + template.Name)),
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
					new CodePrimitiveExpression(template.Name),
					new CodePrimitiveExpression(template.Content));

				var attribute = new CodeAttributeDeclaration(csMenuItemAttributeType.Type,
					new CodeAttributeArgument(new CodePrimitiveExpression("Assets/Create/" + template.Name)),
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