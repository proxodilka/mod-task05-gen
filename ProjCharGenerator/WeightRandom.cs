using System;
using System.Collections.Generic;
using System.Text;

namespace generator
{
    class WeightRandom
    {
        public Random rnd;
        public WeightRandom()
        {
            rnd = new Random();
        }

        public T[] Next<T>(IList<T> values, IList<int> weights=null, int n=1)
        {
            if (weights == null)
            {
                weights = new List<int>(values.Count);
                for (int i=0; i<values.Count; i++)
                {
                    weights.Add(1);
                }
            }
            if (values.Count != weights.Count)
            {
                throw new ArgumentException(
                    $"Lengths of values and weights are not equal: values.Count != weigths.Count ({values.Count} != {weights.Count})"
                );
            }
            T[] result = new T[n];
            int cumsum = 0;
            foreach (int x in weights)
            {
                cumsum += x;
            }
            for (int j = 0; j < n; j++)
            {
                int raw_res = rnd.Next(0, cumsum);
                int cumsum_ = 0;
                bool found_segment = false;
                for (int i = 0; i < weights.Count; i++)
                {
                    if (raw_res >= cumsum_ && raw_res < cumsum_ + weights[i])
                    {
                        result[j] = values[i];
                        found_segment = true;
                        break;
                    }
                    cumsum_ += weights[i];
                }
                if (!found_segment)
                {
                    throw new Exception($"Internal error: did not find matched segment. {{raw_res={raw_res}}}");
                }
            }
            return result;
        }
    }
}
