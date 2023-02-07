using Xylia.bns.Modules.DataFormat.Bin;
using Xylia.bns.Util.Sort;

namespace Xylia.bns.Modules.DataFormat.Analyse.Input
{
	/// <summary>
	/// 指针（索引）类型输入结构
	/// </summary>
	public class StringInputCell : InputCell
	{
		#region 构造
		public StringInputCell()
		{

		}

		public StringInputCell(string Info)
		{
			this.Val = Info;
		}

		public StringInputCell(AttributeInfo attribute) : base(attribute)
		{

		}
		#endregion


		/// <summary>
		/// 明文文本
		/// </summary>
		public string Info => this.Val;

		/// <summary>
		/// 数据长度
		/// </summary>
		public int Size => this.Val.getLength();

		/// <summary>
		/// 索引偏移
		/// </summary>
		public int StringOffset;
	}
}
