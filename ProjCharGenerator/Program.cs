using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Reflection;
using System.Linq;

namespace generator
{
    class CharGenerator 
    {
        private string syms = "абвгдеёжзийклмнопрстуфхцчшщьыъэюя";
        private char[] data;
        private int size;
        private Random random = new Random();
        public CharGenerator() 
        {
           size = syms.Length;
           data = syms.ToCharArray(); 
        }
        public char getSym() 
        {
           return data[random.Next(0, size)]; 
        }
    }

    class EncodedWeights
    {
        public List<string> values;
        public List<int> weights;

        public EncodedWeights(List<int> weights=null, List<string> values=null)
        {
            this.values = values != null? values : new List<string>();
            this.weights = weights != null? weights : new List<int>();
        }
    }

    class BigramGenerator
    {
        Dictionary<string, EncodedWeights> frequency_map = new Dictionary<string, EncodedWeights>();
        string prev_value = "";
        WeightRandom rnd = new WeightRandom();
        public BigramGenerator(string frequency_filepath)
        {
            StreamReader sr = new StreamReader(frequency_filepath);

            Dictionary<string, string> parse_params = new Dictionary<string, string>() {
                ["type"] = "matrix",
                ["sep"] = " "
            };

            while (true)
            {
                string next_str = sr.ReadLine();
                if (next_str == null)
                {
                    throw new Exception(
                        "Incorrect file format: 'data' section is missing."
                    );
                }

                string[] param_container = next_str.ToLower().Split(":");
                string key = param_container[0];
                if (key == "data")
                {
                    break;
                }
                string value = "";
                if (param_container.Length > 1)
                {
                    value = param_container[1];
                }
                if (value != "" || !parse_params.ContainsKey(key))
                {
                    parse_params[key] = value;
                }
            }

            string parser_name = $"Parse{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parse_params["type"])}";
            MethodInfo parser = this.GetType().GetMethod(parser_name);
            
            if (parser == null)
            {
                throw new NotImplementedException(
                    $"{parse_params["type"]} data-type is not supported yet. ({parser_name} is not implemented)"
                );
            }

            parser.Invoke(this, new object[] { sr, parse_params});
        }

        public void ParseMatrix(StreamReader sr, Dictionary<string, string> parse_params)
        {
            string sep = parse_params["sep"];
            while (!sr.EndOfStream)
            {
                string[] next_str = sr.ReadLine().Trim().Split(sep);
                string key = next_str[0];

                List<int> weights = new ArraySegment<string>(next_str, 1, next_str.Length - 1)
                                            .Select(int.Parse)
                                            .ToList();

                frequency_map[key] = new EncodedWeights(weights);
            }

            List<string> values = frequency_map.Keys.ToList();
            foreach (EncodedWeights ew in frequency_map.Values)
            {
                ew.values = values;
            }
        }

        public void ParseList1d(StreamReader sr, Dictionary<string, string> parse_params)
        {
            EncodedWeights ew = new EncodedWeights();
            string sep = parse_params["sep"];
            while (!sr.EndOfStream)
            {
                string[] next_str = sr.ReadLine().Trim().Split(sep);
                int weight = int.Parse(next_str[0]);
                string key = next_str[1];
                ew.values.Add(key);
                ew.weights.Add(weight);
            }
            for (int i=0; i<ew.values.Count; i++)
            {
                frequency_map[ew.values[i]] = ew;
            }
        }

        public void ParseList2d(StreamReader sr, Dictionary<string, string> parse_params)
        {
            string sep = parse_params["sep"];
            while (!sr.EndOfStream)
            {
                string[] next_str = sr.ReadLine().Trim().Split(sep);
                int weight = int.Parse(next_str[0]);
                string key = next_str[1];
                string value = next_str[2];
                if (!frequency_map.ContainsKey(key))
                {
                    frequency_map[key] = new EncodedWeights();
                }
                frequency_map[key].values.Add(value);
                frequency_map[key].weights.Add(weight);
            }
        }

        public string Next(int n, string sep="")
        {
            string[] res = new string[n];
            for (int i=0; i<n; i++)
            {
                res[i] = Next();
            }
            return String.Join(sep, res);
        }

        public string Next()
        {
            string result;
            if (!frequency_map.ContainsKey(prev_value))
            {
                result = rnd.Next(frequency_map.Keys.ToArray())[0];
            }
            else
            {
                EncodedWeights ew = frequency_map[prev_value];
                result = rnd.Next(ew.values, ew.weights)[0];
            }
            prev_value = result;
            return result;
        }
    }

    class Program
    {
        static void ReportGenerated(string filepath, int n, string sep)
        {
            string generated = new BigramGenerator(filepath).Next(n, sep);
            string out_filepath = filepath + "_out";

            StreamWriter sw = new StreamWriter(out_filepath);
            sw.Write(generated);
            sw.Close();
        }
        static void Main(string[] args)
        {
            int N = 1000;
            ReportGenerated(@"../../../Samples/letters-2gram", N, "");
            ReportGenerated(@"../../../Samples/words-1gram", N, " ");
            ReportGenerated(@"../../../Samples/words-2gram", N, " ");
        }
    }
}

