using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xylia.Attribute;

namespace Xylia.bns.Modules.DataFormat.Analyse.Condition
{
	public class NonOut : Condition
	{
		#region 方法
		protected override bool IsMeet(IHash Hash, bool ExistTarget) => false;
		#endregion
	}
}
