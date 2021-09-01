using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ikaroon.CSTT
{
	[FilePath("Ikaroon/CSTT", FilePathAttribute.Location.ProjectFolder)]
	public class ScriptTemplateData : ScriptableSingleton<ScriptTemplateData>
	{
		public IReadOnlyList<ScriptTemplate> Templates { get{ return m_scriptTemplates; } }
		internal List<ScriptTemplate> ScriptTemplates
		{
			get { return m_scriptTemplates; }
			set { m_scriptTemplates = value; }
		}
		[SerializeField]
		List<ScriptTemplate> m_scriptTemplates = new List<ScriptTemplate>();

		public void Save()
		{
			base.Save(false);
		}
	}
}
