using System.Collections.Generic;
using System.Linq;

using Xylia.bns.Modules.DataFormat.Analyse.Record;
using Xylia.bns.Modules.DataFormat.Analyse.Enums;
using Xylia.bns.Modules.DataFormat.Bin.Entity.BDAT;
using Xylia.bns.Modules.DataFormat.Bin.Entity.BDAT.Interface;
using Xylia.bns.Modules.DataFormat.Bin;

namespace Xylia.bns.Util.Sort
{
	/// <summary>
	/// 数据排序 （排序顺序：主id (type:id) => 分id  (alias:id) Short1 => Short2
	/// </summary>
	public class SeriDataSortById : IComparer<IObject>
	{
		/// <summary>
		/// ID类型 记录器
		/// </summary>
		public List<RecordDef> IdRecords;

		public int Compare(IObject x, IObject y)
		{
			#region 初始化
			if (x is null) return -1;
			else if (y is null) return 1;

			BnsId XID = null, YID = null, XLv = null, YLv = null;
			if (x is BDAT_TABLE TableX && y is BDAT_TABLE TableY)
			{
				XID = new BnsId(TableX.Field.ID);
				YID = new BnsId(TableY.Field.ID);

				XLv = TableX.Field.VariationId;
				YLv = TableY.Field.VariationId;
			}
			else if (x is BDAT_FIELDTABLE FieldX && y is BDAT_FIELDTABLE FieldY)
			{
				XID = new BnsId(FieldX.ID);
				YID = new BnsId(FieldX.ID);

				XLv = FieldX.VariationId;
				YLv = FieldX.VariationId;
			}
			#endregion

			#region 处理主ID 拆分排序
			//if (IdRecords != null && IdRecords.Any())
			//{
			//	foreach (var record in IdRecords)
			//	{
			//		switch (record.ExtraType)
			//		{
			//			case ExtraType.Byte1:
			//			{
			//				var Value1 = XID.Byte1;
			//				var Value2 = YID.Byte1;

			//				if (Value1 != Value2) return Value1 - Value2;
			//			}
			//			break;

			//			case ExtraType.Byte2:
			//			{
			//				var Value1 = XID.Byte2;
			//				var Value2 = YID.Byte2;

			//				if (Value1 != Value2) return Value1 - Value2;
			//			}
			//			break;

			//			case ExtraType.Byte3:
			//			{
			//				var Value1 = XID.Byte3;
			//				var Value2 = YID.Byte3;

			//				if (Value1 != Value2) return Value1 - Value2;
			//			}
			//			break;

			//			case ExtraType.Byte4:
			//			{
			//				var Value1 = XID.Byte4;
			//				var Value2 = YID.Byte4;

			//				if (Value1 != Value2) return Value1 - Value2;
			//			}
			//			break;

			//			case ExtraType.Short1:
			//			{
			//				var Value1 = XID.Short1;
			//				var Value2 = YID.Short1;

			//				if (Value1 != Value2) return Value1 - Value2;
			//			}
			//			break;

			//			case ExtraType.Short2:
			//			{
			//				var Value1 = XID.Short2;
			//				var Value2 = YID.Short2;

			//				if (Value1 != Value2) return Value1 - Value2;
			//			}
			//			break;
			//		}
			//	}
			//}
			#endregion

			#region 最后处理
			int XLevel1 = XLv.Short1, YLevel1 = YLv.Short1;
			int XLevel2 = XLv.Short2, YLevel2 = YLv.Short2;

			//根据子属性排序
			if (XID.Main == YID.Main && Equals(XLevel1, YLevel1)) return YLevel2 - XLevel2;
			else if (XID.Main == YID.Main) return XLevel1 - YLevel1;
			else return XID.Main - YID.Main;
			#endregion
		}
	}
}