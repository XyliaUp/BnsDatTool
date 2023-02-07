﻿using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using Xylia.Attribute;
using Xylia.bns.Modules.DataFormat.Analyse.Record;
using Xylia.Extension;

namespace Xylia.bns.Modules.DataFormat.Analyse.Condition
{
	/// <summary>
	/// 条件结构（输出条件，判断条件等）
	/// </summary>
	public class Condition : ICloneable
	{
		#region 字段
		/// <summary>
		/// 条件目标（Alias)	的小写形式
		/// </summary>
		public string TargetAlias;

		/// <summary>
		/// 是否无视错误
		/// （例如当条件目标不存在时不抛出错误）
		/// </summary>
		public bool IgnoreError { get; set; } = true;

		/// <summary>
		/// 返回是否无效
		/// </summary>
		public virtual bool Invalid => true;
		#endregion




		#region 方法
		/// <summary>
		/// 加载参数
		/// </summary>
		/// <param name="Params"></param>
		public virtual void LoadParams(params string[] Params)
		{
			if (Params.Length >= 2)
			{
				//获取处理对象
				this.TargetAlias = Params[1]?.Trim();
				if (string.IsNullOrWhiteSpace(this.TargetAlias))
					throw new ArgumentNullException("TargetAlias为空");
			}
		}

		/// <summary>
		/// 判断是否是特殊表达式
		/// </summary>
		/// <param name="Hash">哈希表</param>
		/// <param name="Result">是否满足表达式</param>
		/// <param name="ExtraParam">额外参数</param>
		/// <returns></returns>
		public bool TryIsMeet(IHash Hash, out bool Result, int? ExtraParam = null)
		{
			if (!this.Invalid)
			{
				Result = this.IsMeet(Hash, ExtraParam);
				return true;
			}

			Result = false;
			return false;
		}

		/// <summary>
		/// 是否符合条件
		/// </summary>
		/// <param name="Hash"></param>
		/// <param name="ExtraParam">额外参数</param>
		/// <returns></returns>
		public bool IsMeet(IHash Hash,int? ExtraParam) => this.IsMeet(Hash, Hash.Contains(this.TargetAlias, ExtraParam));


		protected virtual bool IsMeet(IHash Hash, bool ExistTarget) => throw new NotImplementedException("条件规则无效");


		public override bool Equals(object obj)
		{
			var Cond2 = obj as Condition;

			//如果任意有一个无效，则返回否
			if (this.Invalid || Cond2.Invalid) return false;

			//比对处理类型
			if (this.GetType() != Cond2.GetType()) return false;
			else
			{
				//如果类型相同，比对目标别名
				if (string.IsNullOrWhiteSpace(this.TargetAlias)) throw new Exception("目标别名不能为空");
				else if (!Equals(this.TargetAlias, Cond2.TargetAlias)) return false;

				return true;
			}
		}


		public override int GetHashCode() => TargetAlias.GetHashCode() ^ this.GetType().GetHashCode();
		#endregion




		#region ICloneable
		/// <summary>
		/// 深拷贝
		/// </summary>
		/// <returns></returns>
		public RecordDef DeepClone()
		{
			using var objectStream = new MemoryStream();

			IFormatter formatter = new BinaryFormatter();
			formatter.Serialize(objectStream, this);
			objectStream.Seek(0, SeekOrigin.Begin);
			return formatter.Deserialize(objectStream) as RecordDef;
		}

		public object Clone() => this.MemberwiseClone();
		#endregion
	}


	/// <summary>
	/// 扩展
	/// </summary>
	public static class Util
	{
		public static Condition GetCondition(this string ConditionText)
		{
			#region 初始化
			if (string.IsNullOrWhiteSpace(ConditionText)) return null;

			var group = ConditionText.Split(':');
			string TypeText = group[0]?.Trim();
			if (!TypeText.TryParseToEnum(out ConditionType type, false, true))
			{
				type = TypeText.ToLower() switch
				{
					"ge" => ConditionType.GreaterThanOrEqualTo,
					"gt" => ConditionType.GreaterThan,
					"le" => ConditionType.LessThanOrEqualTo,
					"lt" => ConditionType.LessThan,

					_ => throw new Exception($"条件类型初始化失败 ({ TypeText } => { ConditionText })"),
				};
			}
			#endregion


			#region 实例化
			Condition Result = type switch
			{
				ConditionType.Exist => new Exist(true),
				ConditionType.UnExist => new Exist(false),

				ConditionType.Equal => new Equal(),

				ConditionType.GreaterThan => new Compare(Op.gt),
				ConditionType.GreaterThanOrEqualTo => new Compare(Op.ge),
				ConditionType.LessThan => new Compare(Op.lt),
				ConditionType.LessThanOrEqualTo => new Compare(Op.le),

				ConditionType.NonOut => new NonOut(),

				_ => null,
			};


			if (Result is null) return null;
			else Result.LoadParams(group);

			return Result;
			#endregion
		}
	}
}
