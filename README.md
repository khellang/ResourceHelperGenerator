# ResourceHelperGenerator

An MSBuild task that generates a strongly typed helper class for resource files with support for string formatting.

The task will automatically hook into the MSBuild pipeline when you install the NuGet package, either using `Install-Package ResourceHelperGenerator` or through the Package Manager UI.

It will generate strongly typed helpers for `*.resx` files under the `Properties` folder of your project. This will hopefully be customizable at some point in the future.

String resources containing either `{0}`-type placeholders or `{argumentName}`-type placeholders, will get methods accepting corresponding arguments, while resources without placeholders will get properties generated.

## Example

If you have the following resource file, called `Strings.resx` sitting under the `Properties` folder of your project: 

| Name | Value |
|------|-------|
| ArgumentNull | The argument '{argumentName}' cannot be null. |
| StringArgumentEmpty | The string argument '{argumentName}' cannot be empty. |

The following file, `Strings.Designer.cs` will be generated and placed under the `Strings.resx` file in your project:

```csharp
public static class Strings
{
    private static readonly ResourceManager ResourceManager
        = new ResourceManager("ResourceGenerator.TestProject.Strings", typeof(Strings).Assembly);

    /// <summary>
    /// The argument '{argumentName}' cannot be null.
    /// </summary>
    public static string ArgumentNull(object argumentName)
    {
        return string.Format(CultureInfo.CurrentCulture, GetString("ArgumentNull", "argumentName"), argumentName);
    }

    /// <summary>
    /// The string argument '{argumentName}' cannot be empty.
    /// </summary>
    public static string StringArgumentEmpty(object argumentName)
    {
        return string.Format(CultureInfo.CurrentCulture, GetString("StringArgumentEmpty", "argumentName"), argumentName);
    }

    private static string GetString(string name, params string[] formatterNames)
    {
        var value = ResourceManager.GetString(name);

        Debug.Assert(value != null);

        if (formatterNames != null)
        {
            for (var i = 0; i < formatterNames.Length; i++)
            {
                value = value.Replace("{" + formatterNames[i] + "}", "{" + i + "}");
            }
        }

        return value;
    }
}
```

The first time this happens, the task will modify your project file (to add the helper) and you will be asked to reload the project.
