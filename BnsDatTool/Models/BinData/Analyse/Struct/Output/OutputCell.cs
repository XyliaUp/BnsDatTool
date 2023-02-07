using System;

using Xylia.bns.Modules.DataFormat.Analyse.Record;
using Xylia.bns.Modules.DataFormat.Analyse.Condition;
using Xylia.bns.Modules.DataFormat.Analyse.Value;
using Xylia.Extension;

namespace Xylia.bns.Modules.DataFormat.Analyse.Output
{
	/// <summary>
	/// 信息输出结构单元
	/// </summary>
	public sealed class OutputCell
	{
		#region 构造
		public OutputCell()
		{

		}

		public OutputCell(string Alias, string OutputVal)
		{
			this.Alias = Alias;
			this.OutputVal = OutputVal;
		}
		#endregion

		#region 字段
		/// <summary>
		/// 数据位置（Excel结构使用）
		/// </summary>
		public int Index;

		/// <summary>
		/// 数据名称（一般情况下，用于Excel）
		/// </summary>
		public string Name;

		/// <summary>
		/// 数据别名
		/// </summary>
		public string Alias;

		/// <summary>
		/// 输出数据数值
		/// </summary>
		public string OutputVal;


		/// <summary>
		/// 声明输出条件
		/// </summary>
		public Condition.Condition OutCond;

		/// <summary>
		/// 存在输出条件
		/// </summary>
		public bool HasOutCond => this.OutCond != null && !this.OutCond.Invalid;

		/// <summary>
		/// 重复类型索引
		/// </summary>
		public int? RepeatIndex;
		#endregion



		#region 方法
		public override string ToString() => this.OutputVal;

		/// <summary>
		/// 指示可以输出
		/// </summary>
		/// <param name="Val"></param>
		/// <param name="Record"></param>
		/// <returns></returns>
		public static bool CanOutput(ref object Val, RecordDef Record)
		{
			#region 初始化
			//数值为null时,无需输出
			if (Val is null || !Record.CanOutput) return false;

			//判断输出条件类型
			//如果设置为不输出，则直接返回
			else if (Record.OutCond != null && Record.OutCond is NonOut) return false;



			//判断默认值是否为空
			bool EmptyDefault = true;
			if (Record.DefaultInfo != null) EmptyDefault = string.IsNullOrWhiteSpace(Record.DefaultInfo.Value);
			#endregion

			#region 根据类型处理
			//逻辑类型在读取数值时已判断，不用再处理
			if (Record.ValueType == VType.TBool || Val is bool)
			{
				if (Val is not bool @flag) throw new Exception("类型错误");
				else if ((EmptyDefault && (@flag || Record.ShowEmpty)) || (!EmptyDefault && Record.DefaultInfo.Value.ToBool(out var _tmp) && @flag != _tmp))
				{
					Val = @flag ? "y" : "n";
					return true;
				}

				return false;
			}

			//Float 类型判断
			else if (Record.ValueType == VType.TFloat32)
			{
				if (Val is not float @float) throw new Exception("类型错误");
				else if ((EmptyDefault && (@float != 0 || Record.ShowEmpty)) || (!EmptyDefault && float.TryParse(Record.DefaultInfo.Value, out float _tmp) && @float != _tmp))
				{
					Val = Convert.ToString(@float);
					return true;
				}

				return false;
			}

			//先处理文本值
			else if (Val is string @StrVal)
			{
				//如果无默认值或当前值与默认值不同时允许输出
				if (EmptyDefault &&
					(string.IsNullOrWhiteSpace(@StrVal) || @StrVal == "0" ||

					(Record.HasSeq && Record.Seq.DefaultCell != null && @StrVal.MyEquals(Record.Seq.DefaultCell.Alias)
					)))
					return false;

				else if (!EmptyDefault) return !Record.DefaultInfo.Value.MyEquals(@StrVal);
				else return true;
			}


			else if (Record.ValueType == VType.TInt32 || Val is int)
			{
				if (Val is not int @IntVal) throw new Exception("类型错误");
				else return (EmptyDefault && (@IntVal != 0 || Record.ShowEmpty)) || (!EmptyDefault && int.TryParse(Record.DefaultInfo.Value, out var _tmp) && @IntVal != _tmp);
			}

			else if (Record.ValueType == VType.TInt64)
			{
				if (Val is not long @LongVal) throw new Exception("类型错误");
				else return (EmptyDefault && (@LongVal != 0 || Record.ShowEmpty)) || (!EmptyDefault && long.TryParse(Record.DefaultInfo.Value, out var _tmp) && @LongVal != _tmp);
			}

			else if (Record.ValueType == VType.TInt16
				  || Record.ValueType == VType.TDistance
				  || Record.ValueType == VType.TVelocity
				 )
			{
				if (Val is not short @ShortVal) throw new Exception("类型错误");
				else return (EmptyDefault && (@ShortVal != 0 || Record.ShowEmpty)) || (!EmptyDefault && short.TryParse(Record.DefaultInfo.Value, out var _tmp) && @ShortVal != _tmp);
			}

			else if (Record.ValueType == VType.TInt8)
			{
				if (Val is not sbyte @ByteVal) throw new Exception("类型错误");

				//激活不允许缺省
				if (@ByteVal == 0) return (!EmptyDefault && Record.DefaultInfo.Value != "0") || Record.ShowEmpty;
				else
				{
					var DefaultVal = EmptyDefault ? 0 : sbyte.Parse(Record.DefaultInfo.Value);
					return @ByteVal != DefaultVal;
				}
			}
			#endregion

			//其他情况均允许输出
			return true;
		}
		#endregion
	}
}
