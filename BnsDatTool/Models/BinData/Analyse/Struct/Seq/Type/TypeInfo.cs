﻿using Xylia.bns.Modules.DataFormat.Analyse.Seq;
using Xylia.Files.XmlEx;

namespace Xylia.bns.Modules.DataFormat.Analyse.Type
{
	/// <summary>
	/// 类别记录器补充信息
	/// </summary>
	public sealed class TypeInfo : SeqInfo<TypeCell>
	{

	}


	public static partial class Extension
	{
		/// <summary>
		/// 获取类型信息
		/// </summary>
		/// <param name="seriData"></param>
		/// <param name="TypeInfo"></param>
		/// <returns></returns>
		public static TypeCell GetTypeCell(this XmlProperty seriData, TypeInfo TypeInfo)
		{
			//如果当前数据包含类型字段名
			if (TypeInfo != null && !string.IsNullOrWhiteSpace(TypeInfo.Name) && seriData.Attributes.ContainsName(TypeInfo.Name, out string Value, true))
			{
				var Result = TypeInfo.GetCell(Value);
				if (Result != null) return Result;
			}

			//返回默认对象
			return TypeInfo[-1];
		}
	}
}
