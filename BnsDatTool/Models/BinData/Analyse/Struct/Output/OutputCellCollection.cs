using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

using Xylia.Attribute;
using Xylia.Extension;


namespace Xylia.bns.Modules.DataFormat.Analyse.Output
{
	/// <summary>
	/// 信息输出单元集合体
	/// </summary>
	public class OutputCellCollection : List<OutputCell>, IHash
	{
		#region 字段
		private readonly Hashtable ht = new(StringComparer.Create(CultureInfo.InvariantCulture, true));

		public readonly ObjectOutput Parent;
		#endregion

		#region 构造
		public OutputCellCollection(ObjectOutput Parent) => this.Parent = Parent; 

		public OutputCellCollection(IEnumerable<OutputCell> OutputCells) => this.AddRange(OutputCells);
		#endregion




		#region 方法
		public OutputCell this[string Alias]
		{
			set => this.ht[Alias] = value;
			get
			{
				if(this.ht.ContainsKey(Alias)) 
					return (OutputCell)this.ht[Alias];


				if (Parent?.Data != null)
				{
					Parent.DesObject();
				}

				return null;
			}
		}
		#endregion

		#region 公共接口方法
		public new void Add(OutputCell item)
		{
			if (this.ht.ContainsKey(item.Alias)) System.Diagnostics.Debug.WriteLine($"存在相同名称对象 => " + item.Alias);
			else this.ht.Add(item.Alias, item);

			base.Add(item);
		}

		public new void AddRange(IEnumerable<OutputCell> items)
		{
			foreach (var item in items) this.Add(item);
		}

		public new bool Remove(OutputCell item)
		{
			this.ht.Remove(item.Alias);
			return base.Remove(item);
		}

		public bool Remove(string alias)
		{
			if (this.ht.ContainsKey(alias))
			{
				this.Remove((OutputCell)this.ht[alias]);
				return true;
			}

			return false;
		}

		public new void Clear()
		{
			base.Clear();
			this.ht.Clear();
		}
		#endregion

		#region IHash
		public bool Contains(string Alias, int? ExtraParam = null) => this.ht.ContainsKey(Alias.GetExtraParam(ExtraParam));

		public string GetValue(string Name) => this.Contains(Name) ? this[Name].OutputVal : null;

		string IHash.this[string Name] => GetValue(Name);
		#endregion
	}
}