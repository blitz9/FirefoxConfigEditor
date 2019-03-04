using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FirefoxConfigEditor
{
    internal sealed class Parameter
    {
        public enum ParamType
        {
            delete,
            add,
            unknow
        }

        private Parameter() { }

        public Parameter(string name, string value, ParamType type)
        {
            Name = name;
            Value = value;
            ParameterType = type;
        }

        public string Name { get; set; }
        public string Value { get; set; }
        public ParamType ParameterType { get; set; }

        public string ParamToString() => $"user_pref({Name},{Value});";

        public static explicit operator Parameter(string param)
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

            var name = param.Substring(param.IndexOf('(') + 1,
                                       param.IndexOf(',') - param.IndexOf('(') - 1);

            var value = param.Substring(param.IndexOf(',') + 1,
                                        param.IndexOf(')') - param.IndexOf(',') - 1);

            return new Parameter(name, value, type);
        }
    }

    internal class Program
    {
        private static List<string> ReadAllFromFile(char[] charSeparators, string path)
        {
            var stringsFromFile = new List<string>();

            if (!File.Exists(path))
            {
                Console.WriteLine("File does not exist.");
                return stringsFromFile;
            }

            using (var fstream = File.OpenRead(path))
            {
                var info = new byte[fstream.Length];
                fstream.Read(info, 0, info.Length);
                var textFromFile = System.Text.Encoding.Default.GetString(info);
                stringsFromFile = textFromFile.Split(charSeparators, StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            return stringsFromFile;
        }

        private static void WriteInFile(string path, List<string> listString)
        {
            using (var fstream = new FileStream(path, FileMode.Create))
            {
                var writingString = "";

                foreach (var str in listString)
                {
                    writingString += str + '\n';
                }

                var array = System.Text.Encoding.Default.GetBytes(writingString);
                fstream.Write(array, 0, array.Length);
            }
        }

        private static (List<Parameter> AddedParams, List<Parameter> DeletedParams) LoadRules(char[] charSeparators, string path)
        {

            var addParams = new List<Parameter>();
            var deleteParams = new List<Parameter>();
            var rules = ReadAllFromFile(charSeparators, path);

            foreach (var strParam in rules)
            {
                var newParameter = (Parameter) strParam;
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

        private static void Main(string[] args)
        {
            char[] charSeparators = { '\n' };

            //load new rules
            var ruleFilePath = "rules.txt";

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

            var profileManagerFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                @"\Mozilla\Firefox\profiles.ini";

            var infoAboutProfiles = ReadAllFromFile(charSeparators, profileManagerFile);

            var defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                @"\Mozilla\Firefox\Profiles\";

            var profilePaths = from path in infoAboutProfiles
                               where path.IndexOf("Path") > -1
                               select path.IndexOf("Path=Profiles") > -1 ?
                               path.Replace("Path=Profiles/", defaultPath) :
                               path.Replace("Path=", "");

            foreach (var path in profilePaths)
            {
                try
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
                catch (Exception e)
                {
                    throw new ArgumentNullException($"Error writing to file {path.TrimEnd(new char[] { '\r' }) + @"\prefs.js"} \n", e);
                }
            }
        }
    }
}
