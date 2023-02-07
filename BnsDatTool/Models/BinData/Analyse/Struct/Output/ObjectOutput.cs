using System;

using Xylia.bns.Modules.DataFormat.Analyse.DeSerialize;
using Xylia.bns.Modules.DataFormat.Bin;
using Xylia.bns.Modules.DataFormat.Bin.Entity.BDAT.Interface;

namespace Xylia.bns.Modules.DataFormat.Analyse.Output
{
	/// <summary>
	/// 输出实体
	/// </summary>
	public sealed class ObjectOutput
	{
		#region 字段
		public int Main;

		public BnsId Level;

		/// <summary>
		/// 属性集合
		/// </summary>
		public OutputCellCollection Cells;


		public ObjectOutput()
		{
			this.Cells = new(this);
		}
		#endregion




		#region 预加载模式
		/// <summary>
		/// 已经完整读取
		/// </summary>
		public bool FullLoad = false;

		public IObject Data;

		public DeSerializerTable DeSerializeList;

		/// <summary>
		/// 预加载的对象进行完整读取
		/// </summary>
		public void DesObject()
		{
			if (Data is null) throw new Exception("不是预加载对象");

			DeSerializeList.DesObject(this.Data,  false, false, this);

			this.FullLoad = true;
		}
		#endregion
	}
}
