using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xylia.bns.Modules.DataFormat.Analyse.Enums;
using Xylia.bns.Modules.DataFormat.Analyse.Output;
using Xylia.bns.Modules.DataFormat.Analyse.Record;
using Xylia.bns.Modules.DataFormat.Analyse.Value;
using Xylia.bns.Modules.DataFormat.Analyse.Value.Derive;
using Xylia.bns.Modules.DataFormat.Bin.Entity.BDAT;
using Xylia.bns.Modules.DataFormat.Bin.Entity.BDAT.Interface;
using Xylia.bns.Util.Sort;

using RefValue = Xylia.bns.Modules.DataFormat.Analyse.Value.Derive.Ref;
using TypeValue = Xylia.bns.Modules.DataFormat.Analyse.Value.Derive.Type;

namespace Xylia.bns.Modules.DataFormat.Analyse.DeSerialize
{
	/// <summary>
	/// 数据表 <see langword="反序列化器"/>
	/// </summary>
	public sealed class DeSerializerTable
	{
		#region 字段
		/// <summary>
		/// 反序列化器
		/// </summary>
		public DeSerializer DeSerializer;

		/// <summary>
		/// 数据记录器信息
		/// </summary>
		public TableInfo DataRecord;


		public Dictionary<short, TypeRecordTable> RecordTable;

		public DeSerializerTable(DeSerializer DeSerializer, TableInfo DataRecord, bool is64)
		{
			this.DeSerializer = DeSerializer;
			this.DataRecord = DataRecord;

			//获取记录器组合表
			this.RecordTable = this.DataRecord.GetRecordTable(is64);
		}
		#endregion




		#region 方法
		/// <summary>
		/// 校验长度
		/// </summary>
		/// <param name="TypeKey"></param>
		/// <param name="DataLength"></param>
		/// <returns></returns>
		private bool CheckSize(short TypeKey, int DataLength)
		{
			//类型信息
			string TypeInfo = this.DataRecord.TypeInfo.GetCell(TypeKey)?.Alias ?? TypeKey.ToString();

			//配置长度
			int SetSize = 0;
			if (RecordTable.ContainsKey(TypeKey)) SetSize = RecordTable[TypeKey].Size;
			else
			{
				SetSize = RecordTable[-1].Size;
				Console.WriteLine($"[CheckSize] 类型 { TypeKey } 未定义在类型序列中，按默认类型处理");
			}


			if (DataLength > SetSize)
			{
				Console.WriteLine(
					$"[CheckSize] 类型 { TypeInfo } 仍有剩余数据     " +
					$"配置长度：{ SetSize }  实际长度：{ DataLength }  " +
					$"剩余字节：{ DataLength - SetSize }");

				return false;
			}

			return true;
		}

		/// <summary>
		/// 数据转换为信息
		/// </summary>
		/// <param name="Record"></param>
		/// <param name="SourceData"></param>
		/// <param name="StartOffset"></param>
		/// <returns></returns>
		private object ConvertToInfo(RecordDef Record, IObject SourceData, int StartOffset)
		{
			using var ValueEntity = Record.ValueType.Factory();
			ValueEntity.CurData = SourceData;

			if (ValueEntity is TypeValue @type) @type.TypeInfo = this.DataRecord.TypeInfo;
			else if (ValueEntity is Text @text) @text.LocalData = this.DeSerializer.LocalData;
			else if (ValueEntity is RefValue @ref)
			{
				@ref.GameData = this.DeSerializer.GameData;
				@ref.LocalData = this.DeSerializer.LocalData;
			}

			//获取结果并返回
			return ValueEntity.ReadInfo(SourceData.Field.Data, StartOffset, Record);
		}


		/// <summary>
		/// 反序列化处理数据
		/// </summary>
		/// <param name="lstData"></param>
		/// <param name="Sort">是否按照id进行排序</param>
		/// <param name="Progress"></param>
		/// <returns></returns>
		public List<ObjectOutput> DeSerialize(IEnumerable<IObject> lstData, bool Sort, MessageHandle Progress)
		{
			var Start = DateTime.Now;

			#region 初始化信息
			var Result = new ConcurrentBag<ObjectOutput>();          //由于使用多线程，必须使用线程安全集合

			//计算每一次的进度差，异常处理无数据对象
			IObject[] ListData = null;
			if (lstData.Last() is null)
			{
				if (!this.DataRecord.Rules.Contains(ListRule.extra)) Console.WriteLine($"#cred#[CheckExtra] 当前数据表需要使用额外规则");
				ListData = lstData.Take(lstData.Count() - 1).ToArray();
			}
			else
			{
				if (this.DataRecord.Rules.Contains(ListRule.extra)) Console.WriteLine($"#cred#[CheckExtra] 当前数据表不应该使用额外规则，请进行检查");
				ListData = lstData.ToArray();
			}

			float StepVal = 65.0F / ListData.Length;
			#endregion

			#region 检查数据长度
			if (this.DataRecord != null && this.DataRecord.CheckSize)
			{
				foreach (var (Type, Length) in ListData.Where(o => o != null).Select(g => (g.FType, g.Field.Data.Length)).Distinct().OrderBy(g => g.FType))
					CheckSize(Type, Length);
			}
			#endregion

			LogWriter.WriteLine($"校验耗时 { (DateTime.Now - Start).TotalSeconds }s");



			#region 遍历数据集合执行处理
			Parallel.For(0, ListData.Length, ParallelIdx =>
			{
				this.DeSerializer.ThreadPools.Add(Thread.CurrentThread);

				var CurData = ListData[ParallelIdx];
				if (CurData != null && CurData.HasData)
				{
					var outdata = DesObject(CurData);
					if (outdata != null) Result.Add(outdata);
				}

				Progress.Update(StepVal);
			});
			#endregion

			LogWriter.WriteLine($"生成耗时 { (DateTime.Now - Start).TotalSeconds }s");



			#region 最后处理
			//清理临时资源
			ListData = null;
			RecordTable = null;

			var temp = Result.ToList();
			Result = null;

			//千万不要在此函数销毁 SeriCellInfo => 会导致其他表无法获取外键
			GC.Collect();


			//执行数据排序（由于使用多线程，必须在最后处理）
			if (Sort)
			{
				//var extraType = this.DataRecord.Records.Where(r => r.ValueType == VType.TId && r.ExtraType != ExtraType.None).Select(r => r.ExtraType).ToList();

				//temp.Sort(new OutputSortById()
				//{
				//	HasBool1 = extraType.Contains(ExtraType.Bool1),
				//	HasBool2 = extraType.Contains(ExtraType.Bool2),
				//	HasBool3 = extraType.Contains(ExtraType.Bool3),
				//	HasBool4 = extraType.Contains(ExtraType.Bool4),

				//	HasByte1 = extraType.Contains(ExtraType.Byte1),
				//	HasByte2 = extraType.Contains(ExtraType.Byte2),
				//	HasByte3 = extraType.Contains(ExtraType.Byte3),
				//	HasByte4 = extraType.Contains(ExtraType.Byte4),

				//	HasShort1 = extraType.Contains(ExtraType.Short1),
				//	HasShort2 = extraType.Contains(ExtraType.Short2),
				//});
			}


			LogWriter.WriteLine($"排序耗时 { (DateTime.Now - Start).TotalSeconds }s");


			return temp;
			#endregion
		}

		public ObjectOutput DesObject(IObject CellData, bool Preload = false, bool checkSize = false, ObjectOutput outdata = null)
		{
			#region 获取对象数据
			BDAT_FIELDTABLE CurData = null;

			if (CellData is BDAT_FIELDTABLE @FieldTable) CurData = @FieldTable;
			else if (CellData is BDAT_TABLE @Table) CurData = @Table.Field;
			else throw new NotImplementedException("不支持当前对象");

			if (checkSize && this.DataRecord != null && this.DataRecord.CheckSize) CheckSize(CurData.SubclassType, CurData.Size - 12);
			#endregion

			#region 初始化信息
			if (outdata is null)
			{
				outdata = new ObjectOutput();
				outdata.Main = CurData.ID;
				outdata.Level = CurData.VariationId;

				if (!Preload) outdata.FullLoad = true;
				else
				{
					outdata.Data = CellData;
					outdata.DeSerializeList = this;
				}
			}
			#endregion



			#region 处理数据对象
			//遍历记录器  注意: 处理完成后不能清理记录器
			//获取类型对应的字典
			List<RecordDef> records = null;
			if (RecordTable.TryGetValue(CurData.SubclassType, out var temp)) records = temp.Records;
			else records = RecordTable[-1].Records;
			
			var Cells = new ConcurrentBag<OutputCell>();
			Parallel.ForEach(records, (record, ParallelLoopState) =>
			{
				this.DeSerializer.ThreadPools.Add(Thread.CurrentThread);

				#region 初始判断
				//判断数据是否超出总长度
				if (record.Offset != 0 && record.EndIndex > CurData.Data.Length)
				{
					Console.WriteLine($"[Error] { CurData.ID } ({ this.DataRecord.Type } => { record.Alias }) 数据已经超出。\n" +
						$"(类型: { CurData.SubclassType }, 数据总长度: { CurData.Data.Length }, 当前读取起始位置: { record.Offset })");
					return;
				}
				#endregion

				#region 读取数据
				for (int x = 1; x <= record.Repeat; x++)
				{
					//#region 初始化
					//string RecordAlias = record.GetAlias(CurIndex);
					//int startInfo = record.Offset + (UseIndex ? ((CurIndex - 1) * record.Size) : 0);    //计算起始偏移

					//object Value = this.ConvertToInfo(record, CurData, startInfo);
					//if (!OutputCell.CanOutput(ref Value, record)) return;
					//#endregion

					//#region 数据存储
					//Cells.Add(new OutputCell()
					//{
					//	Alias = RecordAlias,
					//	Name = string.IsNullOrWhiteSpace(record.Name) ? RecordAlias : record.Name,
					//	OutCond = record.OutCond,
					//	OutputVal = Value.ToString(),   // 
					//	RepeatIndex = x                 //UseIndex ? CurIndex : null,
					//}); 
					//#endregion
				}


				//if (Record is RepeatRecord repeatRecord)
				//{
				//	for (int x = repeatRecord.StartNumber; x <= repeatRecord.FinishNumber; x++)
				//		ReadData(true, x, repeatRecord.StartNumber);
				//}
				//else ReadData(false);
				#endregion
			});
			#endregion

			#region 校验数据
			outdata.Cells = new OutputCellCollection(Cells);
			Cells = null;

			//校验输出条件
			var Removes = outdata.Cells.Where(t => t.HasOutCond && !t.OutCond.IsMeet(outdata.Cells, t.RepeatIndex)).ToList();
			Removes.ForEach(r => outdata.Cells.Remove(r));

			//这个排序居然耗费了20几秒
			outdata.Cells.Sort(new OutputCellSort());
			#endregion

			//最后处理
			return outdata;
		}
		#endregion
	}
}