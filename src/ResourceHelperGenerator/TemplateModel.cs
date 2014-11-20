using System.Collections.Generic;

namespace ResourceHelperGenerator
{
    internal class TemplateModel
    {
        public TemplateModel(string projectName, string fileName, IEnumerable<ResourceData> resourceData, bool internalize)
        {
            ProjectName = projectName;
            FileName = fileName;
            ResourceData = resourceData;
            Internalize = internalize;
        }

        public string ProjectName { get; private set; }

        public string FileName { get; private set; }

        public IEnumerable<ResourceData> ResourceData { get; private set; }

        public bool Internalize { get; private set; }
    }
}