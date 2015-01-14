using System.Collections.Generic;

namespace ResourceHelperGenerator
{
    internal class TemplateModel
    {
        public TemplateModel(string projectName, string fileName, IEnumerable<ResourceData> resourceData)
        {
            ProjectName = projectName;
            FileName = fileName;
            ResourceData = resourceData;
        }

        public string ProjectName { get; private set; }

        public string FileName { get; private set; }

        public IEnumerable<ResourceData> ResourceData { get; private set; }
    }
}