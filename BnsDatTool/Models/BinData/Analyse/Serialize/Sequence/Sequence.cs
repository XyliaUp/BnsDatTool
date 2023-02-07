using System;
using System.Collections.Generic;
using System.Xml;

using Xylia.Extension;

namespace Xylia.bns.Modules.DataFormat.Analyse
{
    /// <summary>
    /// 分析工具执行序列
    /// </summary>
    public class Sequence
	{
		#region 字段
		public string Alias;

		public string Name;

		/// <summary>
		/// 步骤集合
		/// </summary>
		public List<SequenceStep> Steps;
		#endregion


		#region 方法
		public static Sequence LoadTest(string Path)
		{
			Console.WriteLine($"当前执行的序列文件是: {Path}");

			XmlDocument XmlDoc = new();
			XmlDoc.Load(Path);
			foreach (XmlElement SequenceNode in XmlDoc.DocumentElement.SelectNodes("./sequence"))
			{
				var result = new Sequence();
				result.Alias = SequenceNode.Attributes["alias"]?.Value;
				result.Name = SequenceNode.Attributes["name"]?.Value;
				result.Steps = new List<SequenceStep>();

				HashSet<string> StepMap = new(StringComparer.OrdinalIgnoreCase);
				foreach (XmlElement StepNode in SequenceNode.SelectNodes("./step"))
				{
					var Step = new SequenceStep();

					Step.Target = StepNode.Attributes["target"]?.Value;
					Enum.TryParse(StepNode.Attributes["type"]?.Value, out Step.Type);
					Step.Retired = StepNode.Attributes["retired"]?.Value.ToBool() ?? false;
					Step.Priority = StepNode.Attributes["priority"]?.Value.ToIntWithNull();

					//校验是否已经存在相同的目标
					if (StepMap.Contains(Step.Target)) throw new Exception("重复对象: " + Step.Target);
					StepMap.Add(Step.Target);

					result.Steps.Add(Step);
				}

				return result;
			}

			return null;
		}
		#endregion
	}
}
