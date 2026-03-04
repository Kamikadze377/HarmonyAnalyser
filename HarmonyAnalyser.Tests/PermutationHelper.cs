using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonyAnalyser.Tests
{
    public static class PermutationHelper
    {
        public static IEnumerable<List<string>> GetPermutations(List<string> list, int length)
        {
            if (length == 1)
                return list.Select(t => new List<string> { t });

            return GetPermutations(list, length - 1)
                .SelectMany(t => list.Where(e => !t.Contains(e)),
                            (t1, t2) => t1.Concat(new List<string> { t2 }).ToList());
        }
    }
}
