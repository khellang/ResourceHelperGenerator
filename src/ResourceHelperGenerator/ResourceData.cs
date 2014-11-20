using System.Collections.Generic;
using System.Linq;

namespace ResourceHelperGenerator
{
    internal class ResourceData
    {
        public string Name { get; set; }

        public string Value { get; set; }

        public List<string> Arguments { get; set; }

        public string Comment { get; set; }

        public bool UsingNamedArgs { get; set; }

        public string FormatArguments
        {
            get { return string.Join(", ", Arguments.Select(arg => string.Format("\"{0}\"", arg))); }
        }

        public string ArgumentNames
        {
            get { return string.Join(", ", Arguments.Select(GetArgName)); }
        }

        public string Parameters
        {
            get { return string.Join(", ", Arguments.Select(arg => string.Format("object {0}", GetArgName(arg)))); }
        }

        private string GetArgName(string name)
        {
            return UsingNamedArgs ? name : string.Concat("arg", name);
        }
    }
}