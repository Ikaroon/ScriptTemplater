using System.Text;
using UnityEngine;

namespace Ikaroon.CSTT
{
	[System.Serializable]
	public class ScriptTemplate
	{
		public string Name
		{
			get { return m_name; }
			internal set { m_name = value; }
		}
		[SerializeField]
		string m_name = "New Script Template";

		public string Content
		{
			get { return m_content; }
			internal set { m_content = value; }
		}
		[SerializeField]
		string m_content;

		public bool Expanded
		{
			get { return m_expanded; }
			internal set { m_expanded = value; }
		}
		[SerializeField]
		bool m_expanded;

		public string GenerateMethod()
		{
			var content = new StringBuilder();

			content.AppendLine($"[MenuItem(\"Assets/Create/Create {Name}\", false, 81)]");
			content.AppendLine($"static void CreateScript{Name.Replace(" ", "_")}()");
			content.AppendLine("{");
			content.AppendLine("");
			content.AppendLine("}");

			return content.ToString();
		}
	}
}