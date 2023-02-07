using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Xylia.bns.Modules.DataFormat.Analyse.Output;
using Xylia.bns.Modules.DataFormat.Analyse.Server;
using Xylia.bns.Modules.DataFormat.Analyse.XmlRef;
using Xylia.bns.Modules.DataFormat.Bin;
using Xylia.bns.Modules.DataFormat.Bin.Entity.BDAT;
using Xylia.bns.Modules.DataFormat.Bin.Entity.BDAT.Interface;
using Xylia.Extension;
using Xylia.Files;
using Xylia.Files.Excel;
using Xylia.Files.XmlEx;

namespace Xylia.bns.Modules.DataFormat.Analyse.DeSerialize
{
	/// <summary>
	/// <see langword="反序列化器"/>
	/// </summary>
	public class DeSerializer
	{
		#region 字段
		public bool OutAsXml = true;

		/// <summary>
		/// 输出目录路径
		/// </summary>
		public string OutFolderPath { get; set; }

		/// <summary>
		/// 线程池
		/// </summary>
		public BlockingCollection<Thread> ThreadPools = new();


		/// <summary>
		/// 序列化表信息
		/// </summary>
		public BinData GameData;

		/// <summary>
		/// 汉化数据
		/// </summary>
		public TextBinData LocalData;
		#endregion


		#region 校验数据
		public static void CheckData(BinData GameData, params TableInfo[] Lists)
		{
			if (Lists is null) throw new ArgumentNullException("未赋值配置表");

			BlockingCollection<Exception> Exceptions = new();
			Parallel.ForEach(Lists, List =>
			{
				if (List.TargetID == 0)
				{
					if (GameData.ContainsList(List.Type, out var ListID)) List.TargetID = ListID;
					else
					{
						Console.WriteLine(List.Type + " 主数据获取失败，只会生成空文件");
						LogWriter.WriteLine(List.Type + " 主数据获取失败");

						return;
					}
				}

				//校验版本号信息
				var MajorVersion = GameData[List.TargetID].MajorVersion;
				var MinorVersion = GameData[List.TargetID].MinorVersion;
				if (MajorVersion != List.ConfigMajorVersion ||
				   MinorVersion != List.ConfigMinorVersion)
				{
					string RealVer = $"{ MajorVersion }.{ MinorVersion}";
					string ConfigVer = $"{ List.ConfigMajorVersion }.{ List.ConfigMinorVersion}";
					
					if (List.CheckVersion) Exceptions.Add(new Exception($"[{List.Type} -> {List.TargetID}] 校验失败  配置版本 {ConfigVer} <> 实际版本 {RealVer}"));
					else
					{
						string Msg = $"[{List.Type} -> {List.TargetID}] 配置版本 {ConfigVer} <> 实际版本 {RealVer}";
						Console.WriteLine(Msg);
						LogWriter.WriteLine(Msg);
					}
				}


				#region 校验引用是否存在
				//如果未传递所需处理数据 (本列 & 外键列）
				var InfoList = ListEx.GetAllList(List);
				InfoList.Remove(List.Type);

				foreach (var l in InfoList)
				{
					if (!GameData.ContainsList(l, out var temp))
					{
						string msg = $"☆ 数据表 { l } 获取失败";

						Console.WriteLine(msg);
						LogWriter.WriteLine(msg);
					}
				}
				#endregion
			});


			//如果存在异常
			if (Exceptions.Any())
			{
				var Msg = Exceptions.Aggregate(string.Empty, (sum, now) => sum + now.Message + "\n");
				if (MessageBox.Show($"似乎存在一些问题，是否仍然继续进行？\n由于剑灵的数据结构会随版本不断更新，需及时进行适配。\n{Msg}", "版本号校验异常提醒", MessageBoxButtons.YesNo) != DialogResult.Yes)
				{
					//在此处抛出所有校验异常
					if (Exceptions.Count == 1) throw Exceptions.First();
					else if (Exceptions.Count > 1) throw new AggregateException(Exceptions);
				}
			}
		}
		#endregion

		#region 输出对象
		public void Execute(string DataPath, string LocalDataPath, List<TableInfo> Lists, DeSerializerParam param = null)
		{
			if (Lists != null && Lists.Count == 0) throw new NullReferenceException("没有载入任何有效的配置数据");
			if (DataPath is null) throw new ArgumentNullException("未赋值文件路径");

			//载入完成后执行xml解析
			var GameData = new BinData(DataPath);
			XmlCache.LoadXmlRef(Lists, DataPath);

			//加载汉化数据
			var LocalData = new TextBinData(LocalDataPath);

			Execute(GameData, LocalData, Lists, param);
		}

		public void Execute(List<TableInfo> Lists, DeSerializerParam param = null) => this.Execute(this.GameData, this.LocalData, Lists, param);


		public void Execute(BinData GameData, TextBinData LocalData, List<TableInfo> Lists, DeSerializerParam param = null)
		{
			#region 初始化
			if (OutFolderPath.IsNull()) throw new Exception("请设置数据存储目录");
			else if (!Directory.Exists(OutFolderPath)) Directory.CreateDirectory(OutFolderPath);


			CheckData(GameData, Lists.ToArray());

			//当前执行进度
			MessageHandle CurProcess = new(0, param?.Action);
			CurProcess.Update(10);


			this.GameData = GameData;
			this.LocalData = LocalData;
			#endregion


			#region 开始处理数据 (含有多个表时，进度提示有些异常)
			//使用多线程处理，可能会占用大量资源。如有必要，可以考虑强制限制线程数。
			Parallel.ForEach(Lists, List =>
			{
				#region 线程初始化
				ThreadPools.Add(Thread.CurrentThread);

				//初始 10%，总进度 5%
				CurProcess.Update(5 / Lists.Count);

				//TargetID 在数据校验时获取
				if (List.TargetID == 0) return;
				var Start = DateTime.Now;
				LogWriter.WriteLine("当前正在解析: " + List);
				#endregion

				#region 处理并输出对象
				try
				{
					//初始化数据表反序列器
					var ListDes = new DeSerializerTable(this, List, GameData.Bit64);

					//处理数据 【初始 15%，总进度 80%】
					var Table = GameData[List.TargetID];
					var Objs = Table.CellDatas();
					LogWriter.WriteLine($"{ List.Type } 获取数据耗时 { (DateTime.Now - Start).TotalSeconds }s");

					var data = ListDes.DeSerialize(Objs, true, CurProcess);
					LogWriter.WriteLine($"{ List.Type } 处理数据耗时 { (DateTime.Now - Start).TotalSeconds }s");

					this.Output(List, Table.MajorVersion, Table.MinorVersion, data, param, CurProcess, Lists.Count * data.Count);
					LogWriter.WriteLine($"{ List.Type } 输出数据耗时 { (DateTime.Now - Start).TotalSeconds }s");

					//输出数据 【初始 95%，总进度 100%】
					CurProcess.Update(5 / Lists.Count);
				}
				catch (Exception ee)
				{
					//告知上级函数发生异常的数据表
					throw new Exception($"{ List } 解析异常", ee);
				}
				#endregion
			});
			#endregion

			#region 资源清理
			Console.WriteLine("输出已结束");

			param?.Action?.Invoke(100);
			if (param?.ClearData ?? true)
			{
				GameData.Dispose();
				GameData = null;
			}

			ThreadPools = new BlockingCollection<Thread>();
			GC.Collect();
			#endregion
		}



		private void Output(TableInfo List, ushort MajorVersion, ushort MinorVersion,
			List<ObjectOutput> data, DeSerializerParam param, MessageHandle Progress, int MaxVal)
		{
			#region 初始化输出结构
			IFile FileEntity = null;

			//客户端记录器
			var ClientRecords = List.Records.Where(r => r.Client).ToList();

			if (OutAsXml) FileEntity = List.CreateXml(MajorVersion, MinorVersion);
			else
			{
				FileEntity = new ExcelInfo("数据");

				//创建标题行对象
				var TitleRow = ((ExcelInfo)FileEntity).sheet.CreateRow(0);

				//设置标题行内容
				for (int x = 0; x < ClientRecords.Count; x++)
				{
					var Record = ClientRecords[x];

					//如果输出 alias 或 name为空，则输出alias
					string TitleName = (param?.OutNameIsAlias == true || string.IsNullOrWhiteSpace(Record.Name)) ? Record.Alias : Record.Name;

					//如果仍然为空，则显示为起始索引
					if (string.IsNullOrWhiteSpace(TitleName))
						TitleName = Record.Offset.ToString();

					//创建行基本信息(标题行）
					var Cell = ((ExcelInfo)FileEntity).CreateCell(TitleRow, x);
					Cell.SetCellValue(TitleName);
				}
			}
			#endregion


			#region 数据处理
			//空数据处理
			if (data.Count == 0) Progress.Update(15);
			else
			{
				int Row = 1;
				float StepVal = (float)15 / MaxVal;     //计算阶段递增值

				//输出数据 【初始 80%，总进度 95%】
				//Parallel.ForEach(data,a =>
				data.ForEach(a =>
				{
					//进度提示
					Progress.Update(StepVal);


					// Xml输出方式处理
					if (FileEntity is XmlInfo XI)
					{
						var Record = XI.CreateElement("record");
						foreach (var cell in a.Cells)
						{
							try
							{
								//CDATA类型判断
								//if (cell.CDATA)
								//{
								//	//Record.AppendChild(XI.CreateCData(cell.OutputVal));
								//	Record.InnerText = cell.OutputVal;
								//}
								////.Replace(" ", "-") 是为防止出现空格导致存储错误的情况
								//else 
									
									Record.SetAttribute((param?.OutNameIsAlias == false ? cell.Name : cell.Alias).Replace(" ", "-"), cell.OutputVal);

							}
							catch (Exception ee)
							{
								Console.WriteLine("[SetAttribute] " + ee.Message + " (Alias = " + cell.Alias + " => Text = " + cell.OutputVal + ")");
							}
						}

						lock (XI) XI.AppendChild(Record);
					}

					//Excel输出方式处理
					else if (FileEntity is ExcelInfo EI)
					{
						//创建行基本信息
						var CurRow = EI.CreateRow(Row++);
						for (int x = 0; x < ClientRecords.Count; x++)
						{
							var Record = ClientRecords[x];

							if (a.Cells.Contains(Record.Alias))
							{
								var Cell = a.Cells[Record.Alias];

								try
								{
									var c = EI.CreateCell(CurRow, x);

									//激活自动换行风格
									c.CellStyle.WrapText = true;
									c.SetCellValue(Cell.OutputVal);
								}
								catch (Exception ee)
								{
									Console.WriteLine($"[SetAttribute] { ee.Message } (Alias: { Record.Alias } => Text: { Cell?.OutputVal })");
								}
							}
						}
					}
				});
			}

			//销毁对象
			data.Clear();
			data = null;
			#endregion

			#region 最终输出存储
			var RelativePath = List.RelativePath ?? List.Type.TitleCase() + "Data";    //如果相对存储路径未设置且已设置表别名，则自动追加 Data

			//输出数据 【初始 95%，总进度 100%】
			RelativePath = RelativePath.Replace(".*", null).Replace("*", null);
			if (OutAsXml) FileEntity.Save(OutFolderPath, RelativePath + ".xml", true);
			else
			{
				//存储文件名（不含扩展名）
				string SaveFileName = OutFolderPath + @"\" + RelativePath;

				//设置最大重试次数
				int RetryTime = 10;
				for (int i = 1; i <= RetryTime; i++)
				{
					try
					{
						if (i != 1) FileEntity.Save(SaveFileName + $" ({ i }).xlsx");
						else FileEntity.Save(SaveFileName + ".xlsx");

						//执行成功结束循环
						break;
					}
					catch (Exception ee)
					{
						//如果最后一次仍未保存成功
						if (i == RetryTime) throw ee;
						else Console.WriteLine($"正在尝试保存文件，但是{ ee.Message } (第{ i }次)");
					}
				}
			}
			#endregion


			//清理文件
			FileEntity.Dispose();
			FileEntity = null;
		}
		#endregion





		public BDAT_LIST GetList(TableInfo TableInfo)
		{
			CheckData(GameData, TableInfo);
			if (TableInfo.TargetID == 0) return null;

			return GameData[TableInfo.TargetID];
		}

		public ObjectOutput GetObject(BDAT_LIST ListData, TableInfo TableInfo, int MainID, int Variation = 0)
		{
			//获得特定数据
			var CellData = ListData.GetObject(MainID, Variation);
			if (CellData is null) return null;

			//返回数据
			var DeSerializeList = new DeSerializerTable(this, TableInfo, GameData.Bit64);
			return DeSerializeList.DesObject(CellData, false, true);
		}

		public IEnumerable<ObjectOutput> GetObjects(BDAT_LIST ListData, TableInfo TableInfo, bool Preload = false)
		{
			IEnumerable<IObject> CellDatas = null;
			if (ListData is BDAT_ARCHIVE Archive) CellDatas = Archive.Tables;
			else if (ListData is BDAT_LOOSE Loose) CellDatas = Loose.Fields.Where(o => o != null);


			//返回数据
			var DesHandle = new DeSerializerTable(this, TableInfo, GameData.Bit64);
			return CellDatas.Select(o => DesHandle.DesObject(o, Preload));
		}
	}
}