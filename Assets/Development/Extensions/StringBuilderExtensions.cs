using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DotsNav
{
    static class StringBuilderExtensions
    {
        public static string Concat<T>(this StringBuilder sb, IEnumerable<T> objects, string separator = ", ")
        {
            if (objects == null)
                return string.Empty;

            var l = objects as IList<T> ?? objects.ToArray();
            if (!l.Any())
                return string.Empty;

            sb.Clear();
            sb.Append(l[0]);

            for (var i = 1; i < l.Count; i++)
            {
                var o = l[i];
                sb.Append(separator);
                sb.Append(o);
            }

            return sb.ToString();
        }
    }
}