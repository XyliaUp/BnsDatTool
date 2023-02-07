using System;

namespace Xylia.bns.Modules.DataFormat.Analyse.DeSerialize
{
	public class DeSerializerParam
	{
		public Action<float> Action = null;

		public bool? OutNameIsAlias = null;

		public bool ClearData = true;
	}
}