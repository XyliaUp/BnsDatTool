﻿using System;
using System.Collections.Generic;
using System.Linq;

using Xylia.bns.Modules.DataFormat.Analyse.Record;
using Xylia.bns.Modules.DataFormat.Analyse.Seq;

namespace Xylia.bns.Modules.DataFormat.Analyse.Type
{
	/// <summary>
	/// 类型信息
	/// </summary>
	public sealed class TypeCell : SeqCell
	{
		#region 构造
		public TypeCell(SeqCell SeqCell) : this(SeqCell.Key, SeqCell.Alias, SeqCell.Name) { }

		public TypeCell(short TypeVal, string TypeAlias, string TypeName = null) : base(TypeVal, TypeAlias, TypeName) { }
		#endregion



		#region 字段
		private Dictionary<string, RecordDef> _records;

		public Dictionary<string, RecordDef> Records
		{
			get => this._records;
			set
			{
				this._records = value;

				//this.DispelRecords = value.Values.Where(r => r.ValueType == Value.VType.TDispelFlag).ToList();
				//if (this.DispelRecords.Count > 1)
				//	throw new Exception("暂不支持多对象筛选");


				////尝试获取存在 Following 字段的记录器
				//this.ChildrenRecords = new();
				//var FollowingTypes = value.Values.Where(f => f is RepeatRecord repeatRecord && repeatRecord.Children is not null).Select(r => (RepeatRecord)r);
				//if (FollowingTypes.Any())
				//{
				//	foreach (var following in FollowingTypes)
				//	{
				//		//校验参数
				//		if (following.CanInput && following.Client)
				//		{
				//			foreach (var f in following.Children)
				//			{
				//				if (ChildrenRecords.ContainsKey(f)) throw new Exception($"Following 冲突 !!！({f})");
				//				else ChildrenRecords.Add(f, following);
				//			}
				//		}
				//	}
				//}
			}
		}


		public List<RecordDef> DispelRecords;



		//public Dictionary<string, RepeatRecord> ChildrenRecords;
		//public bool ContainsChildrenRecord(string Key, out RepeatRecord record, bool CheckClient = false)
		//{
		//	record = null;
		//	if (ChildrenRecords is null || ChildrenRecords.Count == 0) return false;

		//	//查询结果并返回
		//	var flag = ChildrenRecords.ContainsKey(Key);
		//	if (flag)
		//	{
		//		record = ChildrenRecords[Key];
		//		if (CheckClient && !record.Client) return false;
		//	}

		//	return flag;
		//}
		#endregion
	}
}