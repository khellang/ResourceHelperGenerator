using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ResourceHelperGenerator
{
    public class GenerateResourceHelpers : Task
    {
        private static readonly XNamespace MsBuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

        private static readonly Regex NamedParameterMatcher = new Regex(@"\{([a-z]\w+)\}", RegexOptions.IgnoreCase);

        private static readonly Regex NumberParameterMatcher = new Regex(@"\{(\d+)\}");

        [Required]
        public ITaskItem[] Resources { get; set; }

        [Required]
        public string ProjectFile { get; set; }

        public string Namespace { get; set; }

        public bool Internalize { get; set; }

        public override bool Execute()
        {
            try
            {
                var resourceFiles = Resources.Select(x => x.GetMetadata("FullPath"));

                var projectPath = ProjectFile;

                if (string.IsNullOrEmpty(Namespace))
                {
                    Namespace = Path.GetFileNameWithoutExtension(projectPath);
                }

                Log.LogMessage("Using namespace '{0}'", Namespace);

                var document = XDocument.Load(projectPath);

                var shouldSave = false;

                foreach (var resourceFile in resourceFiles)
                {
                    Log.LogMessage("Generating helper for {0}...", Path.GetFileName(resourceFile));

                    if (!TryGenerateResourceFile(resourceFile, Namespace, Internalize))
                    {
                        continue;
                    }

                    var hasAddedFiles = AddDesignerFileToProject(document, resourceFile);

                    shouldSave = shouldSave || hasAddedFiles;
                }

                if (shouldSave)
                {
                    document.Save(projectPath);
                }
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
            }

            return !Log.HasLoggedErrors;
        }

        private bool AddDesignerFileToProject(XContainer document, string resourceFile)
        {
            var hasAddedFiles = false;

            foreach (var embeddedResource in document.Descendants(MsBuildNamespace + "EmbeddedResource"))
            {
                var path = embeddedResource.Attribute("Include").Value;

                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);

                var designerFileName = string.Concat(fileNameWithoutExtension, ".Designer.cs");

                var compileElements = document.Descendants(MsBuildNamespace + "Compile").ToList();

                var elementExists = compileElements
                    .Select(x => x.Attribute("Include").Value)
                    .Any(x => x.EndsWith(designerFileName));

                if (elementExists)
                {
                    break;
                }

                var directory = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(directory))
                {
                    continue;
                }

                if (!resourceFile.EndsWith(path))
                {
                    continue;
                }

                var fileName = Path.GetFileName(path);

                var designerFilePath = Path.Combine(directory, designerFileName);

                Log.LogMessage("Adding designer file '{0}' to project...", designerFilePath);

                var parent = compileElements.Select(x => x.Parent).First();

                var compileElement = CreateCompileElement(designerFilePath, fileName);

                parent.Add(compileElement);

                hasAddedFiles = true;
            }

            return hasAddedFiles;
        }

        private static XElement CreateCompileElement(string designerFilePath, string resourceFileName)
        {
            var compileElement = new XElement(MsBuildNamespace + "Compile");
            compileElement.SetAttributeValue("Include", designerFilePath);

            var autoGenElement = new XElement(MsBuildNamespace + "AutoGen");
            autoGenElement.SetValue("True");
            compileElement.Add(autoGenElement);

            var designTimeElement = new XElement(MsBuildNamespace + "DesignTime");
            designTimeElement.SetValue("True");
            compileElement.Add(designTimeElement);

            var dependentUponElement = new XElement(MsBuildNamespace + "DependentUpon");
            dependentUponElement.SetValue(resourceFileName);
            compileElement.Add(dependentUponElement);

            return compileElement;
        }

        private static bool TryGenerateResourceFile(string resourceFile, string @namespace, bool internalize)
        {
            var resourceData = GetResourceData(resourceFile).ToList();

            var resourceDirectory = Path.GetDirectoryName(resourceFile);

            if (string.IsNullOrEmpty(resourceDirectory))
            {
                return false;
            }

            var resourceFileName = Path.GetFileNameWithoutExtension(resourceFile);

            var templateModel = new TemplateModel(@namespace, resourceFileName, resourceData, internalize);

            var designerFilePath = Path.Combine(resourceDirectory, string.Concat(resourceFileName, ".Designer.cs"));

            using (var stream = File.Create(designerFilePath))
            using (var writer = new StreamWriter(stream))
            {
                TemplateRenderer.RenderTemplate(writer, templateModel);
            }

            return true;
        }

        private static IEnumerable<ResourceData> GetResourceData(string resourceFile)
        {
            using (var reader = new ResXResourceReader(resourceFile) { UseResXDataNodes = true })
            {
                foreach (DictionaryEntry entry in reader)
                {
                    var node = (ResXDataNode) entry.Value;
                    var value = (string) node.GetValue((ITypeResolutionService) null);

                    var usingNamedArgs = true;

                    var match = NamedParameterMatcher.Matches(value);
                    if (match.Count == 0)
                    {
                        usingNamedArgs = false;
                        match = NumberParameterMatcher.Matches(value);
                    }

                    var arguments = match.Cast<Match>()
                        .Select(m => m.Groups[1].Value)
                        .Distinct();

                    if (!usingNamedArgs)
                    {
                        arguments = arguments.OrderBy(Convert.ToInt32);
                    }

                    yield return new ResourceData
                    {
                        Name = node.Name,
                        Value = value,
                        Comment = node.Comment,
                        Arguments = arguments.ToList(),
                        UsingNamedArgs = usingNamedArgs
                    };
                }
            }
        }
    }
}