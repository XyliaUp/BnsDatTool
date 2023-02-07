using System.Collections.Concurrent;

using Xylia.bns.Modules.DataFormat.Analyse.Record;
using Xylia.bns.Modules.DataFormat.Analyse.Input;
using Xylia.bns.Modules.DataFormat.Analyse.Type;
using Xylia.bns.Util.Sort;
using Xylia.Files.XmlEx;

namespace Xylia.bns.Modules.DataFormat.Analyse.Struct
{
	/// <summary>
	/// 序列数据
	/// </summary>
	public class SeriData
	{
		public SeriData(XmlProperty property, bool RuleDispel = false)
		{
			this.Property = property;
			this.RuleDispel = RuleDispel;
		}


		#region 字段
		/// <summary>
		/// 此实例是否使用清理规则
		/// </summary>
		public bool RuleDispel;

		/// <summary>
		/// 属性原始实例
		/// </summary>
		public XmlProperty Property;

		/// <summary>
		/// 类型数值
		/// </summary>
		public TypeCell TypeInfo;

		/// <summary>
		/// 属性信息
		/// </summary>
		public AttributeCollection AttributeInfos = new();

		/// <summary>
		/// 基础结构值
		/// </summary>
		public InputBasicInfo BasicInfo = new();

		/// <summary>
		/// [自动] 编号信息，只应在无ID传入时使用
		/// </summary>
		public int TableIndex = new();


		///// <summary>
		///// 字段使用数量统计
		///// </summary>
		//public ConcurrentDictionary<RepeatRecord, BlockingCollection<Attribute.MyAttribute>> ChildrenRecords = new();
		#endregion
	}
}
