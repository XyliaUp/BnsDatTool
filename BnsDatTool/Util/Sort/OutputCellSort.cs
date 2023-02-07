using System.Collections.Generic;

using Xylia.bns.Modules.DataFormat.Analyse.Output;
using Xylia.Extension;
using Xylia.Sort;

namespace Xylia.bns.Util.Sort
{
	/// <summary>
	/// 属性值排序
	/// </summary>
	public class OutputCellSort : IComparer<OutputCell>
	{
		public int Compare(OutputCell x, OutputCell y)
		{
			// 1在后，-1在前
			if (x.Alias.IsNull() || y.Alias.IsNull()) return 0;

			return Method.StrCompare(x.Alias, y.Alias, Method.SortRule.Common | Method.SortRule.IgnoreCase);
		}
	}
}
