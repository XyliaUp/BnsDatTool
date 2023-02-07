using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xylia.Extension;

namespace Xylia.bns.Modules.DataFormat.Analyse.Serialize
{
	public static class DataLogger
	{
		/// <summary>
		/// 调试输出：写入Field
		/// </summary>
		/// <param name="data"></param>
		/// <param name="Id"></param>
		/// <param name="type"></param>
		public static void Field_Write(this byte[] data, int Id, int? type)
		{
			return;

			if (DebugSwitch.DataWrite)
			{
				LogWriter.WriteLine("-----------------\n" + data.ToHex(), 2);
				LogWriter.WriteLine($"成功写入数据: { Id }, 类型：{ (type is null ? "Null" : type.ToString()) }", 2);
			}
		}

		public static void Field_Add(this byte[] data, int Id, int? type, bool Enabled = false)
		{
			return;

			if (DebugSwitch.DataWrite || Enabled)
			{
				LogWriter.WriteLine("-----------------\n" + data.ToHex(), 2);
				LogWriter.WriteLine($"成功新增数据: { Id }{ (type is null ? null : ", 类型：" + type)  }", 2);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="data"></param>
		/// <param name="Id"></param>
		/// <param name="Enabled"></param>
		public static void Field_Modify(this byte[] data, int Id, bool Enabled = false)
		{
			return;

			if (DebugSwitch.DataWrite || Enabled)
			{
				LogWriter.WriteLine("-----------------\n" + data.ToHex(), 2);
				LogWriter.WriteLine("成功修改Field:" + Id, 2);
			}
		}
	}
}
