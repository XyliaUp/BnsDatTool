using System;
using System.Collections.Generic;

using Xylia.bns.Modules.DataFormat.Bin;

namespace Xylia.bns.Modules.DataFormat.Analyse.Output
{
	public class OutputSortById : IComparer<ObjectOutput>
	{
		public bool HasShort1;
		public bool HasShort2;

		public bool HasByte1;
		public bool HasByte2;
		public bool HasByte3;
		public bool HasByte4;

		public bool HasBool1;
		public bool HasBool2;
		public bool HasBool3;
		public bool HasBool4;


		public int Compare(ObjectOutput x, ObjectOutput y)
		{
			if (x is null) return -1;
			else if (y is null) return 1;


			#region 处理主ID 拆分排序
			var MainDataX = BitConverter.GetBytes(x.Main);
			var MainDataY = BitConverter.GetBytes(y.Main);

			if (HasShort1 && Check(MainDataX.GetShort1(), MainDataY.GetShort1(), out int Result)) return Result;
			if ((HasByte1 || HasBool1) && Check(MainDataX.GetByte1(), MainDataY.GetByte1(), out Result)) return Result;
			if ((HasByte2 || HasBool2) && Check(MainDataX.GetByte2(), MainDataY.GetByte2(), out Result)) return Result;


			if (HasShort2 && Check(MainDataX.GetShort2(), MainDataY.GetShort2(), out Result)) return Result;
			if ((HasByte3 || HasBool3) && Check(MainDataX.GetByte3(), MainDataY.GetByte3(), out Result)) return Result;
			if ((HasByte4 || HasBool4) && Check(MainDataX.GetByte4(), MainDataY.GetByte4(), out Result)) return Result;
			#endregion


			//如果以上都没有，则按主值排序
			if (Check(x.Main, y.Main, out Result)) return Result;
			else if (Check(x.Level.Short1, y.Level.Short1, out Result)) return Result;
			else if (Check(x.Level.Short2, y.Level.Short2, out Result)) return Result;

			return 0;
		}

		/// <summary>
		/// 校验是否相等
		/// </summary>
		/// <param name="Value1"></param>
		/// <param name="Value2"></param>
		/// <param name="Result"></param>
		/// <returns></returns>
		public bool Check(int Value1, int Value2, out int Result)
		{
			if (Value1 != Value2)
			{
				Result = Value1 - Value2;
				return true;
			}

			Result = 0;
			return false;
		}
	}
}
