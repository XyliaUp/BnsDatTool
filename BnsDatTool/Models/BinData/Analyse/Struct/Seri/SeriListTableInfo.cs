using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Xylia.Attribute;
using Xylia.bns.Modules.DataFormat.Analyse.Enums;
using Xylia.bns.Modules.DataFormat.Analyse.Input;
using Xylia.bns.Modules.DataFormat.Analyse.Record;
using Xylia.bns.Modules.DataFormat.Analyse.Struct;
using Xylia.bns.Modules.DataFormat.Analyse.Type;
using Xylia.bns.Modules.DataFormat.Analyse.Value;
using Xylia.bns.Modules.DataFormat.Bin;
using Xylia.bns.Modules.DataFormat.Bin.Entity.BDAT.Interface;
using Xylia.bns.Util.Sort;
using Xylia.Extension;

namespace Xylia.bns.Modules.DataFormat.Analyse
{
	public class SeriListTableInfo : IDisposable, ISeriCell
	{
		#region 构造
		public SeriListTableInfo(TableInfo TableInfo, List<SeriSourceData> SeriDatas = null)
		{
			this.TableInfo = TableInfo;
			this.DataSource = SeriDatas;
		}
		#endregion

		#region 字段
		public int BeginTarget = 0;

		public int FinishTarget = 0;


		/// <summary>
		/// 记录器
		/// </summary>
		public TableInfo TableInfo;

		/// <summary>
		/// 序列数据源
		/// </summary>
		public List<SeriSourceData> DataSource;

		/// <summary>
		/// 序列数据
		/// </summary>
		public SeriData[] SeriData;

		/// <summary>
		/// 载入反序列化数据  各种数值转换在这里进行处理
		/// </summary>
		public List<Input.Input> Inputs;

		/// <summary>
		/// 长度记录器
		/// </summary>
		private Dictionary<short, TypeRecordTable> DataLength;

		/// <summary>
		/// 别名表
		/// </summary>
		private ConcurrentDictionary<string, IdInfo> AliasTable;
		#endregion



		#region 方法
		/// <summary>
		/// 先行处理数据
		/// </summary>
		public void Handle(BinData SeriListInfo, TextBinData LocalInfo, MessageHandle ParentMsg)
		{
			//载入数据信息
			this.LoadAttrInfo(LocalInfo, SeriListInfo, null);


			var ListName = this.TableInfo.Type;
			if (!SeriListInfo.ContainsList(ListName, out var ListID)) throw new Exception("获取表失败");

			//将原始数据转换为Input结构
			this.Inputs = this.LoadSeriData(this.TableInfo.TypeInfo, SeriListInfo, ListName, ParentMsg).ToList();
		}


		/// <summary>
		/// 载入反序列化数据
		/// </summary>
		/// <param name="TypeInfo"></param>
		/// <param name="ListInfo"></param>
		/// <param name="ListName"></param>
		/// <param name="ParentMsg"></param>
		/// <returns></returns>
		private BlockingCollection<Input.Input> LoadSeriData(TypeInfo TypeInfo, BinData ListInfo, string ListName, MessageHandle ParentMsg)
		{
			#region 信息初始化
			var result = new BlockingCollection<Input.Input>();    //输入信息数组
			Hashtable ErrorRecord = new();   //错误信息记录	 主键 => 错误类型
			MessageHandle SeriMsg = new(this.SeriData.Length, ParentMsg);
			#endregion

			#region 载入反序列化数据文件
			Parallel.ForEach(this.SeriData, Case =>
			{
				#region 信息初始化
				SeriMsg.Update();

				if (!this.SeriData.Contains(Case))
					throw new Exception("[Error] 测试，未能在 LoadSelfData时 读取成功");

				//获取当前类型信息
				var CurType = Case.Property.GetTypeCell(TypeInfo);
				BlockingCollection<InputCell> tmpCells = new();

				var input = new Input.Input();  //输入信息
				input.BasicInfo = Case.BasicInfo;  //传递基本信息
				#endregion


				#region
				bool Dispose = false;
				void Handle(AttributeInfo Attr, ParallelLoopState State)
				{
					#region 处理基础信息
					//不处理ID对象	
					if (Attr.Record.ExtraType == ExtraType.ID) return;

					//处理索引类型		   
					else if (Attr.Record.ValueType == VType.TString || Attr.Record.ValueType == VType.TNative)
					{
						var Test = new StringInputCell(Attr);
						if (false && Test.Record.IsAliasRecord) input.BasicInfo.Alias = Test.Val = Test.Val.Replace("GB_", "SEW_");

						tmpCells.Add(Test);
						return;
					}


					//传入数值
					string AttrVal = Attr.Attribute.Value?.ToString().Trim();

					//type分类直接退出 (无需转换）
					//必须给Cells传递分属性，否则数据处理阶段会获取不到type
					if (Attr.Record.ExtraType == ExtraType.Type)
					{
						byte[] tmpVal = new byte[4];
						if (int.TryParse(AttrVal, out int r)) tmpVal = BitConverter.GetBytes(r);     //数值
						else if (!string.IsNullOrWhiteSpace(AttrVal) && Attr.Record.Seq.GetKey(AttrVal, out var key, Attr.Record.UseInfo)) tmpVal = BitConverter.GetBytes((int)key);   //文本类型转换

						//存储数据
						tmpCells.Add(new InputCell()
						{
							Record = Attr.Record,        //用于自定义id排序
							Alias = Attr.Attribute.Name, //属性名
							InputVal = tmpVal,           //做一次转换，后续还要转为int处理
						});

						input.BasicInfo.SubclassType = BitConverter.ToInt16(tmpVal, 0);
						return;
					}
					#endregion

					#region 一般数据处理部分，如果返回异常则直接跳过后续处理	
					var Status = Attr.ToData(ListInfo, ListName, out var Data);
					if (Status == 1) return;   //返回指示失败 
											   //返回指示销毁
					else if (Status == -1)
					{
						Dispose = true;
						State.Stop();
					}
					//即使返回转换成功，也要校验数据是否有效
					else if (Status == 0 && Data is null) return;
					#endregion

					#region 最后处理
					//备注：通过alias在数据处理中获得位置信息
					tmpCells.Add(new InputCell(Attr)
					{
						InputVal = Data, //索引1
					});
					#endregion
				}

				//先处理销毁类型对象
				if (Case.AttributeInfos.DisposeType.Count != 0) Parallel.ForEach(Case.AttributeInfos.DisposeType, (Attr, State) => Handle(Attr, State));

				//执行对象销毁
				if (Dispose) return;

				//遍历已初始处理后的属性
				Parallel.ForEach(Case.AttributeInfos.Where(Attr => Attr.Attribute.HasValue && Attr.Record.ErrorType != ErrorType.Dispose), (Attr, State) => Handle(Attr, State));

				input.Cells = tmpCells.ToList();
				input.Cells.Sort(new InputCellSort());

				tmpCells = null;
				Case.AttributeInfos.Clear();
				#endregion


				#region 获取数据长度并存储
				if (this.DataLength.ContainsKey(CurType.Key)) input.DataLength = this.DataLength[CurType.Key].Size;
				else
				{
					//如果自动长度计算器不包含此分类，抛出错误
					string ErrorKey = CurType.Key + "_" + CrashType.NotIncludedType;

					//此错误记录应该只显示一次
					//排查Type范围，如果存在则表示默认长度，不进行错误提示
					if (!ErrorRecord.Contains(ErrorKey))
					{
						ErrorRecord.Add(ErrorKey, CurType.Key);
						Console.WriteLine($"系统发现类型范围组中未定义类别：{ CurType.Key } ({ CurType.Key })，按默认类别进行处理");
					}

					input.DataLength = this.DataLength[-1].Size;
				}

				result.Add(input);
				#endregion
			});
			#endregion

			this.SeriData = null;
			return result;
		}

		/// <summary>
		/// 载入反序列化配置数据
		/// </summary>
		/// <param name="Local"></param>
		/// <param name="SeriListInfo"></param>
		/// <param name="MessageHandle"></param>
		private void LoadAttrInfo(TextBinData Local, BinData SeriListInfo, MessageHandle MessageHandle)
		{
			#region 信息初始化
			float rangeMin = 10, rangeMax = 50, Current = 0;
			var CrashedAttr = new BlockingCollection<string>();  //记录发生错误的字段名

			//根据配置文件的filter字段生成类型对应记录器字典
			var RecordTable = this.DataLength = this.TableInfo.GetRecordTable(SeriListInfo.Bit64, r => r != null);

			//创建记录器集合
			foreach (var type in this.TableInfo.TypeInfo)
			{
				Dictionary<string, RecordDef> CurRecords = new(StringComparer.InvariantCultureIgnoreCase);
				foreach (var tr in RecordTable[type.Key].Records)
				{
					if (!CurRecords.ContainsKey(tr.Alias)) CurRecords.Add(tr.Alias, tr);
					else Trace.WriteLine($"[debug1] {this.TableInfo.Type}->{tr.Alias} 已存在");
				}

				type.Records = CurRecords;
			}
			#endregion




			#region	载入反序列化数据文件
			this.SeriData = this.DataSource.SelectMany(Seri =>
			{
				//判断此表是否需要清理部分字段
				bool RuleDispel = Seri.Xml.DocumentElement.Attributes["rule-dispel"]?.Value.ToBoolWithNull() ?? false;

				return Seri.Xml.DocumentElement.SelectNodes(".//record").Properties().Select(p => new SeriData(p, RuleDispel));

			}).ToArray();

			//清理资源
			this.DataSource.Clear();

			//赋值自动编号
			for (int i = 0; i < SeriData.Length; i++) SeriData[i].TableIndex = i;
			#endregion

			#region 获取特殊的记录器字典
			var IndexRecords = new Dictionary<int, IEnumerable<RecordDef>>();
			foreach (var r in RecordTable) IndexRecords.Add(r.Key, r.Value.Records.Where(v => (v.ValueType == VType.TString || v.ValueType == VType.TNative) && v.Client));


			var LocalRecords = new Dictionary<int, IEnumerable<RecordDef>>();
			//foreach (var r in RecordTable) LocalRecords.Add(r.Key, r.Value.Records.Where(v => v.ValueType == VType.TTextAlias && v.Client));
			if (LocalRecords.Sum(r => r.Value.Count()) > 0 && Local is null) throw new Exception("汉化文件未载入");

			//默认信息记录器字典
			var DefaultInfos = new Dictionary<int, IEnumerable<RecordDef>>();
			foreach (var r in RecordTable) DefaultInfos.Add(r.Key, r.Value.Records.Where(a => !a.Alias.IsNull() && a.Client && a.DefaultInfo != null));
			#endregion

			#region 初始处理汉化数据
			Parallel.ForEach(SeriData, CurData =>
			{
				CurData.TypeInfo = CurData.Property.GetTypeCell(this.TableInfo.TypeInfo);  //获取当前类型信息
				CurData.AttributeInfos = new();   //属性信息集合

				//处理汉化别名，查询汉化信息并转为数值类型
				foreach (var record in LocalRecords[CurData.TypeInfo.Key])
				{
					HandleInsert(record, Index =>
					{
						var CurAlias = record.GetAlias(Index);
						if (CurData.Property.Attributes.ContainsName(CurAlias, out string Value, true))
						{
							long Id = Local.GetId(Value, record);
							CurData.AttributeInfos.Add(new AttributeInfo(new MyAttribute(CurAlias, Id), record, Index));
						}
					});
				}
			});
			#endregion


			#region 载入反序列化数据文件
			this.AliasTable = new ConcurrentDictionary<string, IdInfo>();
			Parallel.ForEach(SeriData, CurData =>
			{
				//加载属性信息
				void LoadAttribute(RecordDef CurRecord, MyAttribute attr, int Index = 0)
				{
					//if (CurRecord.ValueType == VType.TTextAlias) return;   //已经处理过的信息不需要执行
					if (!CurRecord.Client || !CurRecord.CanInput) return;  //只读取客户端字段

					//两个判断方法其实可以合并，需要等待后续调整
					//对单独对象增加 RuleDispel 的读取机制
					//判断当前数据是否需要清理字段
					if (CurData.RuleDispel && !CurRecord.NotRuleDispel) return;

					//如果记录器存在清除标记且当前数据已设置了清除特征值
					if (CurRecord.RuleDispel && CurData.TypeInfo.DispelRecords != null && CurData.TypeInfo.DispelRecords.Any())
					{
						//获取当前类型中的清除对象信息
						var TestRecord = CurData.TypeInfo.DispelRecords.First();
						if (CurData.Property.Attributes.ContainsName(TestRecord.Alias)) return;
					}

					//全部校验完成后，执行写入
					lock (CurData.AttributeInfos) CurData.AttributeInfos.Add(new AttributeInfo(attr, CurRecord, Index));
				}

				#region 生成字段信息
				//如果内部文本不为空，查询配置文件中启用cdata模式的字段
				if (CurData.Property.InnerText != null)
				{
					var record = CurData.TypeInfo.Records.Values.FirstOrDefault(r => r.CDATA);
					if (record != null) LoadAttribute(record, new MyAttribute(record.Alias, CurData.Property.InnerText));
				}

				//遍历非空属性
				Parallel.ForEach(CurData.Property.Attributes, attr =>
				{
					//如果值为空则直接返回
					if (attr.Value is null || string.IsNullOrWhiteSpace(attr.Value.ToString())) return;


					#region 一般处理
					//寻找属性对应的配置文件对应的alias   				
					if (CurData.TypeInfo.Records.ContainsKey(attr.Name))
					{
						LoadAttribute(CurData.TypeInfo.Records[attr.Name], attr);
						return;
					}

					//不包含字段时判断是否为 repeat 类型
					else
					{
						string MainKey = attr.Name.RegexReplace(@"-\d*$");
						if (CurData.TypeInfo.Records.ContainsKey(MainKey))
						{
							LoadAttribute(CurData.TypeInfo.Records[MainKey], attr, int.Parse(attr.Name.Remove(0, MainKey.Length + 1)));
							return;
						}

						////判断是否存在子对象记录器
						//else if (CurData.TypeInfo.ContainsChildrenRecord(MainKey, out var Target))
						//{
						//	if (!CurData.ChildrenRecords.ContainsKey(Target))
						//		CurData.ChildrenRecords.GetOrAdd(Target, new BlockingCollection<MyAttribute>());

						//	CurData.ChildrenRecords[Target].Add(attr);
						//	return;
						//}
					}
					#endregion

					#region 仍然失败则进行错误提示
					//错误的alias只需要提示一次
					if (!CrashedAttr.Contains(attr.Name))
					{
						CrashedAttr.Add(attr.Name);

						//retired- 为特殊表达式，表示临时停用字段
						if (!attr.Name.MyStartsWith("retired-"))
						{
							string TypeInfo = CurData.TypeInfo.Key == -1 ? null : $"类型值: ({CurData.TypeInfo.Alias}，{ CurData.TypeInfo.Key }) ";
							Console.WriteLine($"[错误] {CurData.Property.Attributes["alias"]} {TypeInfo}  属性未在配置文件中：{ attr.Name } => { attr.Value }");
						}
					}
					#endregion
				});

				////由于多线程处理会导致重复编号，需要单独处理
				//foreach (var ChildrenRecord in CurData.ChildrenRecords)
				//{
				//	int Index = 1;
				//	foreach (var CurAttr in ChildrenRecord.Value) LoadAttribute(ChildrenRecord.Key, CurAttr, Index++);
				//}
				#endregion

				#region 设置缺省数值
				//填充缺省索引  索引即使未设置，也必须写入配置中
				foreach (var record in IndexRecords[CurData.TypeInfo.Key])
				{
					HandleInsert(record, Index =>
					{
						var CurAlias = record.GetAlias(Index);
						if (CurData.AttributeInfos.Contains(CurAlias)) return;

						//存储数据
						LoadAttribute(record, new MyAttribute(CurAlias, record.DefaultInfo?.Value ?? ""), Index);
					});
				}

				//遍历含有默认值的记录器，生成默认值
				foreach (var record in DefaultInfos[CurData.TypeInfo.Key])
				{
					HandleInsert(record, Index =>
					{
						var CurAlias = record.GetAlias(Index);
						if (CurData.AttributeInfos.Contains(CurAlias)) return;

						#region 存储数据
						//指示是否已经定义条件
						bool Result = false;
						bool HasCond = record.OutCond != null && record.OutCond.TryIsMeet(CurData.AttributeInfos, out Result);

						var DefaultInfo = record.DefaultInfo;
						string OriVal = null;

						//判断是否满足条件
						if (!HasCond || Result)
						{
							switch (DefaultInfo.Symbol)
							{
								//如果是实时文本类型
								case Symbol.Text:
								{
									if (CurData.AttributeInfos.Contains(DefaultInfo.SymbolContent))
									{
										throw new NotImplementedException("Symbol.Text 停用");

										//由于转换处理函数自带去除前后导空格，所以这里不用处理
										//OriVal = DefaultInfo.Extra[0] + AttrInfos[DefaultInfo.SymbolContent] + DefaultInfo.Extra[1];
									}
									else Trace.WriteLine($"[Debug] 未包含目标 " + DefaultInfo.SymbolContent);
								}
								break;

								//一般类型
								case Symbol.None:
								{
									OriVal = DefaultInfo.Value;

									//满足判断条件且正确情况下的数值不为空时
									if (Result && !DefaultInfo.ConditionValue.IsNull()) OriVal = DefaultInfo.ConditionValue;
								}
								break;

								default: throw new Exception("未知错误");
							}
						}

						//如果是None类型  有条件且不满足   不论情况与否，都传递值
						else if (DefaultInfo.Symbol == Symbol.None) OriVal = DefaultInfo.Value;

						//如果结果非空进行存储
						//创建信息
						if (!OriVal.IsNull()) LoadAttribute(record, new MyAttribute(CurAlias, OriVal), Index);
						#endregion
					});
				}
				#endregion


				#region 获取基础信息
				//传递顺序索引
				CurData.BasicInfo.TableIndex = CurData.TableIndex;

				//判断数量,如果存在id控制机制，则不使用自动id
				CurData.BasicInfo.MainId = CurData.AttributeInfos.Mains.Any()
						? Value.Derive.Id.CreateMainData(CurData.AttributeInfos.Mains, out CurData.BasicInfo.IsDisposed)
						: new BnsId((int)this.TableInfo.AutoStart + CurData.TableIndex);
				#endregion

				#region 生成level信息
				var LevelAttrs = CurData.AttributeInfos.Levels;
				if (LevelAttrs.Any())
				{
					byte[] LevelData = new byte[4];

					//遍历Id记录器信息，生成数据
					foreach (var IdInfo in LevelAttrs)
					{
						byte[] tmpData = null;
						string AttrValue = IdInfo.Attribute.Value.ToString();

						//数值转换
						if (int.TryParse(AttrValue, out int r)) tmpData = BitConverter.GetBytes(r);

						//文本类型转换
						else if (!AttrValue.IsNull())
						{
							var tmp = IdInfo.Record.Seq.Where(t => t.Alias.MyEquals(IdInfo.Attribute.Value.ToString())).FirstOrDefault();
							if (tmp is null)
							{
								Console.WriteLine($"[Error] id转换失败 ({ IdInfo.Attribute.Value })");
								continue;
							}
							else tmpData = BitConverter.GetBytes(tmp.Key);
						}

						//缺省时跳过
						else continue;

						//处理额外类型
						//BnsId.SplitWrite(ref LevelData, tmpData, IdInfo.Record.ExtraType);
					}

					LevelAttrs = null;

					//存储id信息
					CurData.BasicInfo.Level = new BnsId(LevelData);
				}
				#endregion


				#region [CreatAlias] 生成别名信息
				if (CurData.AttributeInfos.AliasInfo is not null)
				{
					var Alias = CurData.BasicInfo.Alias = CurData.AttributeInfos.AliasInfo.Attribute.Value.ToString();

					if (string.IsNullOrWhiteSpace(Alias)) LogWriter.WriteLine($"[CreatAlias] 别名不存在 ({this.TableInfo.Type} => {CurData.BasicInfo.TableIndex})");
					else if (!AliasTable.ContainsKey(Alias)) AliasTable.GetOrAdd(Alias, new IdInfo(CurData.BasicInfo.MainId, CurData.BasicInfo.Level));

					//判断是否输出调试日志
					else if (DebugSwitch.CreatAliasGroup) LogWriter.WriteLine($"[CreatAlias] 重复别名 ({this.TableInfo.Type} > { Alias })");
				}
				#endregion
			});
			#endregion
		}

		/// <summary>
		/// 执行插入
		/// </summary>
		/// <param name="record"></param>
		/// <param name="action"></param>
		public void HandleInsert(RecordDef record, Action<int> action)
		{
			for (int x = 1; x <= record.Repeat; x++)
				action(x);


			//if (record is RepeatRecord repeatRecord)
			//{
			//	for (int x = repeatRecord.StartNumber; x <= repeatRecord.FinishNumber; x++) action(x);
			//}
			//else action(0);
		}

		public override string ToString() => $"[{this.GetType()}] {this.TableInfo?.Type}";
		#endregion


		#region ISeriCell
		public bool GetID(string Alias, out IdInfo Info)
		{
			if (!Alias.IsNull() && AliasTable.ContainsKey(Alias))
			{
				var Tar = AliasTable[Alias];

				Info = new IdInfo(Tar.MainId, Tar.VariationId);
				return true;
			}

			//返回失败
			Info = null;
			return false;
		}

		public float Version() => throw new NotImplementedException();

		public IObject GetObject(int MainID, int Variation) => throw new NotImplementedException();

		public IEnumerable<IObject> CellDatas() => throw new NotImplementedException();
		#endregion

		#region IDispose
		private bool disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					this.Inputs.Clear();
					this.DataSource.Clear();

					// TODO: 释放托管状态(托管对象)
				}

				// TODO: 释放未托管的资源(未托管的对象)并重写终结器
				// TODO: 将大型字段设置为 null
				disposedValue = true;
			}
		}

		// // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
		// ~SeriListTableInfo()
		// {
		//     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
		//     Dispose(disposing: false);
		// }
		public void Dispose()
		{
			// 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}