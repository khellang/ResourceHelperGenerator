// <auto-generated />

#if NETFX_CORE
#define RESOURCE_HELPER_TYPEINFO
#endif

namespace MyCompany.AwesomeApp
{
    using System;
    using System.CodeDom.Compiler;
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
    using System.Resources;

    [GeneratedCode("ResourceHelperGenerator", "0.3.2")]
#if RESOURCE_HELPER_INTERNAL
    internal
#else
    public
#endif
    static class Strings
    {
        private static readonly ResourceManager ResourceManager
            = new ResourceManager("MyCompany.AwesomeApp.Properties.Strings", GetAssembly(typeof(Strings)));

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

            if (value == null)
            {
                throw new Exception(string.Format("Value for key '{0}' was null.", name));
            }

            if (formatterNames != null)
            {
                for (var i = 0; i < formatterNames.Length; i++)
                {
                    value = value.Replace("{" + formatterNames[i] + "}", "{" + i + "}");
                }
            }

            return value;
        }

        private static Assembly GetAssembly(Type type)
        {
#if RESOURCE_HELPER_TYPEINFO
            return type.GetTypeInfo().Assembly;
#else
            return type.Assembly;
#endif
        }
    }
}
