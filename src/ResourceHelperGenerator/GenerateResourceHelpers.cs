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

        public override bool Execute()
        {
            try
            {
                var resourceFiles = Resources.Select(x => x.GetMetadata("FullPath")).ToList();
                if (!resourceFiles.Any())
                {
                    Log.LogMessage("No resource files found.");
                    return true;
                }

                if (string.IsNullOrEmpty(Namespace))
                {
                    Namespace = Path.GetFileNameWithoutExtension(ProjectFile);
                }

                Log.LogMessage("Generating helpers using namespace '{0}'", Namespace);

                var document = XDocument.Load(ProjectFile);

                var shouldSave = false;

                foreach (var resourceFile in resourceFiles)
                {
                    Log.LogMessage("Generating helper for '{0}'...", Path.GetFileName(resourceFile));

                    GenerateResourceHelper(resourceFile, Namespace);

                    var hasAddedFiles = AddDesignerFileToProject(document, resourceFile);

                    shouldSave = shouldSave || hasAddedFiles;
                }

                if (!shouldSave)
                {
                    return true;
                }

                new FileInfo(ProjectFile).IsReadOnly = false;

                document.Save(ProjectFile);

                return true;
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
                return false;
            }
        }

        private bool AddDesignerFileToProject(XContainer document, string resourcePath)
        {
            var compileElements = document.Descendants(MsBuildNamespace + "Compile").ToList();

            var hasDesignerFile = compileElements.Any(element => DependsOnResourceFile(element, resourcePath));
            if (hasDesignerFile)
            {
                return false;
            }

            foreach (var embeddedResource in document.Descendants(MsBuildNamespace + "EmbeddedResource"))
            {
                var projectResourcePath = embeddedResource.Attribute("Include").Value;

                var resourceDirectory = Path.GetDirectoryName(projectResourcePath);

                if (string.IsNullOrEmpty(resourceDirectory) || !resourcePath.EndsWith(projectResourcePath))
                {
                    continue;
                }

                var resourceFileName = Path.GetFileNameWithoutExtension(projectResourcePath);

                var designerFileName = string.Concat(resourceFileName, ".Designer.cs");

                var designerFilePath = Path.Combine(resourceDirectory, designerFileName);

                Log.LogMessage("Adding designer file '{0}' to project...", designerFileName);

                // Just pick the first Compile element and add the file to its parent.
                var parent = compileElements.Select(x => x.Parent).First();

                var fileName = Path.GetFileName(projectResourcePath);

                var compileElement = CreateCompileElement(designerFilePath, fileName);

                parent.Add(compileElement);

                return true;
            }

            return false;
        }

        private static bool DependsOnResourceFile(XContainer element, string resourceFile)
        {
            var dependentUponElement = element
                .Descendants(MsBuildNamespace + "DependentUpon")
                .FirstOrDefault();

            return dependentUponElement != null && resourceFile.EndsWith(dependentUponElement.Value);
        }

        private static XElement CreateCompileElement(string designerFilePath, string resourceFileName)
        {
            var compileElement = new XElement(MsBuildNamespace + "Compile");

            compileElement.Add(new XElement(MsBuildNamespace + "AutoGen") { Value = "True" });
            compileElement.Add(new XElement(MsBuildNamespace + "DesignTime") { Value = "True" });
            compileElement.Add(new XElement(MsBuildNamespace + "DependentUpon") { Value = resourceFileName });
            compileElement.SetAttributeValue("Include", designerFilePath);

            return compileElement;
        }

        private static void GenerateResourceHelper(string resourceFile, string @namespace)
        {
            var resourceData = GetResourceData(resourceFile).ToList();
            if (!resourceData.Any())
            {
                return;
            }

            var resourceDirectory = Path.GetDirectoryName(resourceFile);
            if (string.IsNullOrEmpty(resourceDirectory))
            {
                return;
            }

            var resourceFileName = Path.GetFileNameWithoutExtension(resourceFile);

            var templateModel = new TemplateModel(@namespace, resourceFileName, resourceData);

            var designerFilePath = Path.Combine(resourceDirectory, string.Concat(resourceFileName, ".Designer.cs"));

            var designerFile = new FileInfo(designerFilePath);
            if (designerFile.Exists)
            {
                designerFile.IsReadOnly = false;
            }

            using (var stream = File.Create(designerFilePath))
            using (var writer = new StreamWriter(stream))
            {
                TemplateRenderer.RenderTemplate(writer, templateModel);
            }
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