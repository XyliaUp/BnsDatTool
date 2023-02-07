﻿using System.Collections.Generic;

namespace Xylia.bns.Util.Sort.Common
{
	/// <summary>
	/// 通过主键的大小进行排序公共泛型方法
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class SortByKeyNum<T> : IComparer<KeyValuePair<int, T>>
	{
		public int Compare(KeyValuePair<int, T> x, KeyValuePair<int, T> y)
		{
			return x.Key - y.Key;
		}
	}
}
