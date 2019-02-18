using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace FirefoxConfigEditor
{
    sealed class Parameter
    {
        public enum ParamType
        {
            delete,
            add,
            unknow
        }
        Parameter() { }

        public Parameter(string name, string value, ParamType type)
        {
            Name = name;
            Value = value;
            ParameterType = type;
            Name = name;
            Value = value;
            ParameterType = type;
        }

        public string Name { get; set; }
        public string Value { get; set; }
        public ParamType ParameterType { get; set; }

        public string ParamToString()
        {
            return $"user_pref({Name},{Value});";
        }

        public static implicit operator Parameter(string param)
        {
            ParamType type;

            switch (param[0])
            {
                case '+':
                    type = ParamType.add;
                    break;
                case '-':
                    type = ParamType.delete;
                    break;
                default:
                    type = ParamType.unknow;
                    break;
            }

            string name = param.Substring(param.IndexOf('(') + 1, param.IndexOf(',') - param.IndexOf('(') - 1);
            string value = param.Substring(param.IndexOf(',') + 1, param.IndexOf(')') - param.IndexOf(',') - 1);

            return new Parameter(name, value, type);
        }
    }

    class Program
    {
        private static bool IsString(String s1,string s2)
        {
            return s1==s2;
        }

        static List<string> ReadAllFromFile(char[] charSeparators, string path)
        {
            List<string> stringsFromFile = new List<string>();

            if (!File.Exists(path))
            {
                Console.WriteLine("File does not exist.");
                return stringsFromFile;
            }

            using (FileStream fstream = File.OpenRead(path))
            {
                byte[] info = new byte[fstream.Length];
                fstream.Read(info, 0, info.Length);
                string textFromFile = System.Text.Encoding.Default.GetString(info);
                stringsFromFile = textFromFile.Split(charSeparators, StringSplitOptions.RemoveEmptyEntries).ToList<string>();
            }

            return stringsFromFile;
        }

        static void WriteInFile(string path, List<string> writingStrings)
        {
            using (FileStream fstream = new FileStream(path, FileMode.Create))
            {
                string writingString = "";

                foreach (string str in writingStrings)
                {
                    writingString = writingString + str + '\n';
                }
                // запись массива байтов в файл
                byte[] array = System.Text.Encoding.Default.GetBytes(writingString);
                fstream.Write(array, 0, array.Length);
            }
        }

        static (List<Parameter> AddedParams, List<Parameter> DeletedParams) LoadRules(char[] charSeparators, string path)
        {

            List<Parameter> addParams = new List<Parameter>();
            List<Parameter> deleteParams = new List<Parameter>();
            List<string> rules = ReadAllFromFile(charSeparators, path);
            foreach (string strParam in rules)
            {
                Parameter newParameter = strParam;
                switch (newParameter.ParameterType)
                {
                    case Parameter.ParamType.delete:
                        deleteParams.Add(newParameter);
                        break;
                    case Parameter.ParamType.add:
                        addParams.Add(newParameter);
                        break;
                }
            }

            return (addParams, deleteParams);
        }

        static void Main(string[] args)
        {
            char[] charSeparators = { '\n' };

            //load new rules
            string ruleFilePath = "rules.txt";

            if (args.Length > 0)
            {
                if (args.Length > 1)
                {
                    Console.WriteLine("incorrect number of parameters");
                    return;
                }
                ruleFilePath = args[0];
            }

            var rules = LoadRules(charSeparators, ruleFilePath);

            if (rules.AddedParams.Count == 0 && rules.DeletedParams.Count == 0)
            {
                return;
            }

            string profileManagerFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                @"\Mozilla\Firefox\profiles.ini";

            List<string> stringsFromFile = ReadAllFromFile(charSeparators, profileManagerFile);

            string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                @"\Mozilla\Firefox\Profiles\";

            IEnumerable<string> profilePaths = from path in stringsFromFile
                                               where path.IndexOf("Path") > -1
                                               select path.IndexOf("Path=Profiles") > -1 ?
                                               path.Replace("Path=Profiles/", defaultPath) :
                                               path.Replace("Path=", "");

            foreach (string path in profilePaths)
            {
                var parameters = ReadAllFromFile(charSeparators, path.TrimEnd(new char[] { '\r' }) + @"\prefs.js");

                foreach (var rule in rules.AddedParams)
                {
                    parameters.Insert(parameters.Count, rule.ParamToString() + '\r');
                }

                foreach (var rule in rules.DeletedParams)
                {
                    parameters.RemoveAll(param => param == rule.ParamToString() + '\r');
                }

                WriteInFile(path.TrimEnd(new char[] { '\r' }) + @"\prefs.js", parameters);
            }
        }
    }
}
