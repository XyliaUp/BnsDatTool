using System.Collections.Generic;

using Xylia.bns.Modules.DataFormat.Analyse.Record;
using Xylia.bns.Util.Sort;


namespace Xylia.bns.Modules.DataFormat.Analyse.Input
{
	/// <summary>
	/// 信息输入结构单元
	/// </summary>
	public class InputCell
	{
		#region 构造
		public InputCell()
		{

		}

		public InputCell(AttributeInfo attribute)
		{
			this.Alias = attribute.Record.GetAlias(attribute.Index);  //属性名
			this.Record = attribute.Record;                    //用于自定义id排序 与 bin数据生成顺序
			this.Val = attribute.Attribute.Value.ToString();  //用于后续默认值 Cond 使用
			this.Index = attribute.Index;
		}
		#endregion



		#region 字段
		/// <summary>
		/// 记录器
		/// </summary>
		public RecordDef Record;

		public int Index;

		/// <summary>
		/// 数据别名（一般情况下，用于Xml）
		/// </summary>
		public string Alias;

		/// <summary>
		/// 用于判断Cond规则
		/// </summary>
		public string Val;

		/// <summary>
		/// 数值 
		/// </summary>
		public byte[] InputVal;

		/// <summary>指示是否关闭缺省设置</summary>
		public bool Default;

		/// <summary>默认数值
		/// 激活后，当数值等于此字段时将不会输出
		/// </summary>
		public string DefaultVal;

		/// <summary>声明输出条件</summary>
		public Condition.Condition OutCondition;
		#endregion

		#region 方法
		/// <summary>
		/// 获得当前偏移
		/// </summary>
		/// <returns></returns>
		public int GetOffset() => this.Record.GetOffset(this.Index);
		#endregion
	}

	public class InputCellSort : IComparer<InputCell>
	{
		public int Compare(InputCell x, InputCell y) => new RecordSort().Compare(x.Record, y.Record);
	}
}
