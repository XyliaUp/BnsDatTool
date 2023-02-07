﻿namespace Xylia.bns.Modules.DataFormat.Analyse.Seq
{
	/// <summary>
	/// 枚举单元
	/// </summary>
	public class SeqCell
	{
		#region 字段
		/// <summary>
		/// 主键
		/// </summary>
		public short Key;

		/// <summary>
		/// 别名
		/// </summary>
		public string Alias;

		/// <summary>
		/// 名称
		/// </summary>
		public string Name;
		#endregion


		#region 构造
		public SeqCell()
		{

		}

		public SeqCell(short Key, string Alias, string Name = null)
		{
			this.Key = Key;
			this.Alias = Alias;
			this.Name = Name ?? Alias;
		}
		#endregion


		public override string ToString() => this.Alias;
	}
}