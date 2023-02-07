using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xylia.bns.Modules.DataFormat.Analyse
{
	/// <summary>
	/// 队列步骤
	/// </summary>
	public class SequenceStep
	{
		public bool Retired;

		public string Target;

		public SequenceStepType Type;

	   /// <summary>
	   /// 处理优先级, 数值越高则处理越在前
	   /// </summary>
		public int? Priority;
	}

	public enum SequenceStepType
	{
		 Table,
	}
}
