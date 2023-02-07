﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Xylia.bns.Modules.DataFormat.Bin.Entity.BDAT.AliasTable
{
	/// <summary>
	/// 别名集合
	/// </summary>
	public sealed class AliasCollection : List<AliasInfo>
	{
		#region 字段
		/// <summary>
		/// 已经确认指向表
		/// </summary>
		public bool HasCheck = false;
		#endregion

		#region 方法
		private readonly Hashtable ht = new(StringComparer.Create(CultureInfo.InvariantCulture, true));

		public AliasInfo this[string alias] => this.ht.ContainsKey(alias) ? (AliasInfo)this.ht[alias] : null;

		public new void Add(AliasInfo aliasInfo)
		{
			base.Add(aliasInfo);

			//由于之前的处理会按表进行拆分，此处无需缓存完整文本
			this.ht[aliasInfo.Alias] = aliasInfo;
		}

		/// <summary>
		/// 重新排序
		/// </summary>
		public void Sort(bool Mode = false) => this.Sort(new HNodeSort(Mode));
		#endregion
	}

	public class HNodeSort : IComparer<AliasInfo>
	{
		#region 构造
		/// <summary>
		/// 指示是否是序列化模式
		/// </summary>
		bool _mode = false;

		public HNodeSort(bool Mode) => _mode = Mode;
		#endregion


		public int Compare(AliasInfo x, AliasInfo y)
		{
			if (x is null || y is null) throw new ArgumentException("参数无效");

			if (_mode) return Sort.Method.StrCompare(x.CompleteText, y.CompleteText, Xylia.Sort.Method.SortRule.EveryChar);
			else return (int)(x.MainID - y.MainID);
		}
	}
}