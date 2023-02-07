using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xylia.bns.Modules.DataFormat.Analyse.XmlRef;
using Xylia.bns.Modules.DataFormat.Bin;

namespace Xylia.bns.Modules.DataFormat.Analyse.Serialize
{
	/// <summary>
	/// 序列化执行器
	/// </summary>
	public sealed class SerializeExecute
	{
		#region 字段
		/// <summary>
		/// 汉化文件路径
		/// </summary>
		public string LocalDataPath;

		/// <summary>
		/// 配置文件路径
		/// </summary>
		public string CurDatPath;

		/// <summary>
		/// 序列化信息
		/// </summary>
		private BinData SeriListInfo;

		/// <summary>
		/// 汉化数据
		/// </summary>
		private TextBinData LocalInfo;

		/// <summary>
		/// 线程池
		/// </summary>
		public BlockingCollection<Thread> ThreadPools = new();
		#endregion


		#region 载入数据
		/// <summary>
		/// 加载游戏数据
		/// </summary>
		/// <param name="TableInfo"></param>
		/// <returns></returns>
		public async Task<List<string>> LoadGData(IEnumerable<TableInfo> TableInfo)
		{
			try
			{
				Console.WriteLine("--------------------------------\n开始提取数据，请耐心等待");

				//载入外键关联表 (注意：不可以加载序列化目标数据）
				return ListEx.GetAllList(TableInfo);
			}
			catch (Exception ee)
			{
				Tip.Message("序列化时异常。\n\n" + ee, "系统错误");
				throw ee;
			}
		}

		/// <summary>
		/// 加载Xml文件外键
		/// </summary>
		/// <returns></returns>
		public async Task LoadXmlRef(IEnumerable<TableInfo> TableInfo) => XmlCache.LoadXmlRef(TableInfo, CurDatPath);




		/// <summary>
		/// 部分序列化
		/// </summary>
		/// <param name="Lists"></param>
		/// <param name="act"></param>
		public void PartSerialize(IEnumerable<SeriListTableInfo> Lists, Action<float, string> act = null)
		{
			#region 信息初始化
			DateTime TotalTime = DateTime.Now, PartTime = DateTime.Now;
			if (!Lists.Any()) throw new Exception("没有载入任何配置文件，请检查文件名是否正确");

			//总数量
			int TotalCount = Lists.Count();

			//加载游戏数据
			this.SeriListInfo = new BinData(CurDatPath);
			if (SeriListInfo is null) throw new ArgumentException("SeriListInfo参数无效");

			//加载汉化数据
			this.LocalInfo = new TextBinData(LocalDataPath);

			LogWriter.WriteLine($"加载游戏数据完成: {DateTime.Now.Subtract(PartTime).TotalSeconds:0.00}s");
			Parallel.ForEach(Lists, List =>
			{
				//获取目标表名
				var Target = this.SeriListInfo.GetList(List.TableInfo.Type);
				if (!Target.HasValue || !this.SeriListInfo._content.Lists.ContainsKey(Target.Value))
					throw new Exception($"未包含处理目标 => { List.TableInfo.Type }");
			});
			#endregion

			#region 异步读取 [10% ~ 40%] 
			var TableInfo = Lists.Select(l => l.TableInfo);
			Task GameData = Task.Run(() => this.LoadGData(TableInfo)), LoadXmlFiles = Task.Run(() => this.LoadXmlRef(TableInfo));

			//等待读取任务完成
			while (!GameData.IsCompleted || !LoadXmlFiles.IsCompleted) Thread.Sleep(1000);

			var PartTime2 = DateTime.Now;
			var Msg = new MessageHandle(TotalCount, 40, 10);

			//载入序列化的配置信息
			foreach (var group in Lists.GroupBy(s => s.TableInfo.Priority).OrderBy(s => s.Key))
			{
				Parallel.ForEach(group, List =>
				{
					this.ThreadPools.Add(Thread.CurrentThread);
					List.Handle(this.SeriListInfo, this.LocalInfo, Msg);

					Msg.Update();
				});
			}

			LogWriter.WriteLine($"载入反序列化数据完成: { DateTime.Now.Subtract(PartTime2).TotalSeconds:0.0}s");
			#endregion

			#region 数据处理 [40% ~ 90%] 
			act?.Invoke(40, "文件读取完成");
			PartTime = DateTime.Now;

			//只处理存在转换结果的对象
			var Msg2 = new MessageHandle(TotalCount, 90, 80);
			Parallel.ForEach(Lists.Where(l => l.Inputs is not null), List =>
			{
				//开始进行数据处理
				if (TotalCount == 1) act?.Invoke(40 + 60 / TotalCount, "处理准备完成，开始重写数据");
				this.SeriListInfo.HandlePart(List, Msg2);

				Msg2.Update();
				act?.Invoke(Msg2.CurrentValue, "重构数据中");

				List.Dispose();
			});
			#endregion

			#region 执行封包 [90% ~100%] 
			act?.Invoke(90, "正在封包中");
			this.SeriListInfo.Compress(CurDatPath, false, BinData.PacketMode.Common);
			act?.Invoke(100, "全部完成");

			GC.Collect();
			#endregion
		}
		#endregion
	}
}
