using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

using Xylia.Attribute;
using Xylia.bns.Modules.DataFormat.Analyse.Enums;
using Xylia.bns.Modules.DataFormat.Analyse.Record;
using Xylia.bns.Modules.DataFormat.Analyse.Value;
using Xylia.bns.Modules.DataFormat.Bin;
using Xylia.Extension;


namespace Xylia.bns.Util.Sort
{
	/// <summary>
	/// 属性信息结构
	/// </summary>
	public class AttributeInfo
	{
		#region 构造
		public AttributeInfo(MyAttribute Attr, RecordDef Record)
		{
			this.Attribute = Attr;
			this.Record = Record;
		}

		public AttributeInfo(MyAttribute Attr, RecordDef Record, int Index)
		{
			this.Attribute = Attr;
			this.Record = Record;
			this.Index = Index;
		}
		#endregion

		#region 字段
		/// <summary>
		/// 序列化传入值
		/// </summary>
		public MyAttribute Attribute;

		/// <summary>
		/// 配置数据
		/// </summary>
		public RecordDef Record;

		/// <summary>
		/// 重复索引, 从1开始
		/// </summary>
		public int Index;

		public string RecordAlias => this.Record.GetAlias(this.Index);
		#endregion


		#region 方法
		/// <summary>
		/// 根据类型，转换原始数据
		/// </summary>
		/// <param name="Info"></param>
		/// <param name="CurListName"></param>
		/// <param name="Data"></param>
		/// <returns>状态值 0=成功，1=失败，-1=销毁</returns>
		public sbyte ToData(BinData Info, string CurListName, out byte[] Data)
		{
			var Type = this.Record.ValueType;
			string OriginalVal = this.Attribute.Value?.ToString()?.Trim();

			#region 初始化
			if (Type == VType.TString || Type == VType.TNative) throw new Exception("无法处理指针类型");
			if (string.IsNullOrWhiteSpace(OriginalVal))
			{
				Trace.WriteLine($"原始值不能为空 ({ this.Record.Alias },{ Type })");

				Data = null;
				return 1;
			}


			//确认无需转换数值的类型
			if (this.Record != null /* (&& this.Record.ValueType == VType.TEnabled || this.Record.ValueType == VType.TType)*/)
			{
				//Hex => Bytes
				Data = BitConverter.GetBytes(int.Parse(OriginalVal));
				return 0;
			}
			#endregion


			#region 实例化数值对象
			var ValueEntity = Type.Factory();
			if (ValueEntity is Ref RefType)
			{
				RefType.CurListName = CurListName;
				RefType.GameData = Info;
			}

			//生成数据
			Data = ValueEntity.WriteBuffer(OriginalVal, this);
			return ValueEntity.Status;
			#endregion
		}
		#endregion
	}


	/// <summary>
	/// 属性集合
	/// </summary>
	public class AttributeCollection : List<AttributeInfo>, IHash
	{
		#region 字段
		public readonly Hashtable ht_alias = new(StringComparer.Create(CultureInfo.InvariantCulture, true));

		/// <summary>
		/// 别名信息
		/// </summary>
		public AttributeInfo AliasInfo;

		/// <summary>
		/// 编号信息
		/// </summary>
		public List<AttributeInfo> Mains = new();

		/// <summary>
		/// 等级信息
		/// </summary>
		public List<AttributeInfo> Levels = new();

		/// <summary>
		/// 销毁类型对象
		/// </summary>
		public List<AttributeInfo> DisposeType = new();


		public bool Test = true;
		#endregion

		#region 方法
		public new void Add(AttributeInfo attribute)
		{
			void Handle()
			{
				if (attribute.Record.IsAliasRecord) this.AliasInfo = attribute;
				//else if (attribute.Record.ValueType == VType.TId) this.Mains.Add(attribute);
				//else if (attribute.Record.ValueType == VType.TLevel) this.Levels.Add(attribute);

				//处理1
				if (attribute.Record.ErrorType == ErrorType.Dispose)
					DisposeType.Add(attribute);

				base.Add(attribute);

				//获取字段别名
				var Alias = attribute.RecordAlias;

				if (this.Contains(Alias)) Trace.WriteLine("重复字段: " + Alias);
				else ht_alias.Add(Alias, attribute);
			}


			if (Test) lock (this) Handle();
			else Handle();
		}

		public new void AddRange(IEnumerable<AttributeInfo> collection)
		{
			foreach (var a in collection) this.Add(a);
		}

		public new void Clear()
		{
			base.Clear();
			this.ht_alias.Clear();
		}



		public bool Contains(string Name, int? ExtraParam = null) => this.ht_alias.ContainsKey(Name.GetExtraParam(ExtraParam));

		public string this[string Name] => throw new NotImplementedException();

		public string GetValue(string Name) => ht_alias[Name].ToString();
		#endregion
	}
}
