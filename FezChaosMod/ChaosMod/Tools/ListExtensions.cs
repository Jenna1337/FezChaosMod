using System.Collections.Generic;

namespace FezGame.GameInfo
{
    public static class ListExtensions
	{
		public static void Add<T>(this List<T> list, IEnumerable<T> enumerable)
		{
			if (enumerable == null)
				System.Diagnostics.Debugger.Break();
			list.AddRange(enumerable);
		}
	}

}