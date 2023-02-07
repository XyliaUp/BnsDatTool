using Xylia.bns.Modules.DataFormat.Bin;

namespace Xylia.bns.Modules.DataFormat.Analyse.Input
{
	/// <summary>
	/// 基础输入信息
	/// </summary>
	public sealed class InputBasicInfo
	{
		public byte XmlNodeType = 1;

		public short SubclassType = -1;


		/// <summary>
		/// 主id
		/// </summary>
		public BnsId MainId { get; set; } = new();

		/// <summary>
		/// 副id
		/// </summary>
		public BnsId Level { get; set; } = new();

		/// <summary>
		/// 元素索引 从0开始
		/// </summary>
		public int TableIndex;

		/// <summary>
		/// 用于创建别名表用，新增字段
		/// </summary>
		public string Alias;


		/// <summary>
		/// 指示当前数据应该被销毁
		/// 此字段的处理代码还未完成
		/// </summary>
		public bool IsDisposed;
	}
}
