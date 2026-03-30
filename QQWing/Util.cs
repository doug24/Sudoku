using System;
using System.Collections.Generic;
using System.Text;

namespace QQWingLib
{
    public static class Util
    {
        public static string SerializeCandidates(HashSet<int>[] candidates)
        {
            if (candidates == null) return string.Empty;

            int[] masks = new int[candidates.Length];
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] != null)
                {
                    foreach (int val in candidates[i])
                        masks[i] |= (1 << val);
                }
            }
            return string.Join(",", masks);
        }

        public static HashSet<int>[] DeserializeCandidates(string candidateMasksCsv)
        {
            if (string.IsNullOrEmpty(candidateMasksCsv)) return null;

            string[] masks = candidateMasksCsv.Split(','); 

            HashSet<int>[] candidates = new HashSet<int>[masks.Length];
            for (int i = 0; i < masks.Length; i++)
            {
                if (int.TryParse(masks[i], out int  mask))
                if (mask != 0)
                {
                    candidates[i] = [];
                    for (int val = 1; val <= 9; val++)
                    {
                        if ((mask & (1 << val)) != 0)
                            candidates[i].Add(val);
                    }
                }
            }
            return candidates;
        }
    }
}
