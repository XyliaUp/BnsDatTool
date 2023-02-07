using System;
using System.Xml;

namespace Xylia.bns.Modules.DataFormat.Analyse.Struct
{
	/// <summary>
	/// 序列化数据文件原始信息
	/// </summary>
	public class SeriSourceData : IDisposable
	{
		#region 构造
		public SeriSourceData(string FilePath)
		{
			this.FilePath = FilePath;

			try
			{
				this.Xml.Load(FilePath);
			}
			catch (Exception e)
			{
				throw new Exception(FilePath, e);
			}
		}
		#endregion

		#region 字段
		/// <summary>
		/// xml实例
		/// </summary>
		public XmlDocument Xml = new();

		/// <summary>
		/// 文件路径
		/// </summary>
		public string FilePath;
		#endregion



		#region IDisposable
		private bool disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: 释放托管状态(托管对象)

					this.Xml = null;
				}

				// TODO: 释放未托管的资源(未托管的对象)并重写终结器
				// TODO: 将大型字段设置为 null
				disposedValue = true;
			}
		}

		// // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
		// ~SeriSourceData()
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
