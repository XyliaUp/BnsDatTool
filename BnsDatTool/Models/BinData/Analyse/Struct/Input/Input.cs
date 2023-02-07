using System.Collections.Generic;
using System.Linq;

using Xylia.bns.Modules.DataFormat.Analyse.Serialize;

namespace Xylia.bns.Modules.DataFormat.Analyse.Input
{
	/// <summary>
	/// 输出
	/// </summary>
	public sealed class Input
	{
		#region 字段
		/// <summary>
		/// 属性集合
		/// </summary>
		public List<InputCell> Cells = new();

		/// <summary>
		/// 明文数据
		/// </summary>
		public List<StringInputCell> Lookups => Cells.Where(c => c is StringInputCell).Select(c => (StringInputCell)c).ToList();

		/// <summary>
		/// 基础结构值
		/// </summary>
		public InputBasicInfo BasicInfo = new();

		/// <summary>
		/// 数据长度
		/// </summary>
		public int DataLength;
		#endregion

		#region 方法
		/// <summary>
		/// 构建数据
		/// </summary>
		/// <param name="IsCompress"></param>
		/// <returns></returns>
		public byte[] StructureData(bool IsCompress)
		{
			// 这里还是会存在覆盖的情况，需要逐个查看
			// 依靠排序覆盖才实现正常生成
			//string Alias = this.Input.Cells.Find(c => c.Alias == "alias").Val;
			//if(Alias == "961_CH_PC_SunDoWonQuest_0001")
			//{
			//	this.Input.Cells.ForEach(c => System.Diagnostics.Trace.WriteLine($"{Alias}   [{c.Record.Alias}]   {c.Record.Start}     {c.Val} => {c.InputVal.ToHex(true)} "));
			//}

			var data = new byte[this.DataLength];   //初始化对象
			this.Cells.ForEach(c => c.StructureData(ref data, IsCompress));  //获取数据单元集合
			return data;
		}
		#endregion
	}
}
