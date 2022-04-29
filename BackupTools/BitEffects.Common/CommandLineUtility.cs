using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BitEffects
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class AliasAttribute : Attribute
    {
        public string Alias { get; }

        public AliasAttribute(string alias)
        {
            this.Alias = alias;
        }
    }

    /// <summary>
    /// Interface for manually converting CLI parameters when loading program options.
    /// </summary>
    public interface IConvertOptions
    {
        /// <summary>
        /// Manually parse an option for this object
        /// </summary>
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        /// <returns>true if the option was parsed</returns>
        bool Convert(string optionName, string[] optionValues);
    }

    static public class CommandLineUtility
    {
        /// <summary>
        /// Convert a CLI list of parameters into an options objects
        /// </summary>
        /// <typeparam name="TOption"></typeparam>
        /// <param name="argv"></param>
        /// <returns></returns>
        static public TOption ParseOptions<TOption>(string[] argv)
        {
            var res = Activator.CreateInstance<TOption>();
            var options = CreateOptionDictionary(argv);

            // First, set all the fields
            foreach (var field in typeof(TOption).GetFields())
            {
                trySetValue(field, field.FieldType, (value) => field.SetValue(res, value));
            }

            // Next, set all the properties
            foreach (var prop in typeof(TOption).GetProperties())
            {
                if (prop.CanWrite == false) continue;
                trySetValue(prop, prop.PropertyType, (value) => prop.SetValue(res, value));
            }

            void trySetValue(MemberInfo mi, Type memberType, Action<object> setValue)
            {
                string key = FindKey(options, mi);
                if (!key.IsEmpty())
                {
                    if ((res as IConvertOptions)?.Convert(mi.Name, options[key]) != true)
                    {
                        if (memberType.IsArray)
                        {
                            Type arrType = memberType.GetElementType();
                            
                            var tmpArray = Array.ConvertAll(options[key], (str) => Convert.ChangeType(str, arrType));
                            var valueArray = Array.CreateInstance(arrType, tmpArray.Length);
                            Array.Copy(tmpArray, valueArray, tmpArray.Length);

                            setValue(valueArray);
                        }
                        else if (memberType == typeof(bool))
                        {
                            setValue(options[key].FirstOrDefault().ToBool(true));
                        }
                        else
                        {
                            string strOpt = string.Join(' ', options[key]);
                            object value = Convert.ChangeType(strOpt, memberType);

                            setValue(value);
                        }
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Find the option name that was used for a specified property.
        /// 
        /// This checks the property name, aliases, and the dashed name, respectively.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        static string FindKey(Dictionary<string, string[]> options, MemberInfo member)
        {
            // First we check just the property name (preferred)
            string[] validNames =
            {
                member.Name,
                member.Name.PascalCased(),
                member.Name.CamelCased()
            };
            foreach (var name in validNames)
            {
                if (options.ContainsKey(name))
                {
                    return name;
                }
            }

            // Next see if there are any aliases for this property
            var aliasAttr = member.GetCustomAttributes<AliasAttribute>()
                .Where(attr => options.ContainsKey(attr.Alias))
                .FirstOrDefault();

            if (aliasAttr != null)
            {
                return aliasAttr.Alias;
            }

            // Convert a camel-cased name to a hyphenated one
            var propName = member.Name.CamelCaseToDashed();
            if (options.ContainsKey(propName))
            {
                return propName;
            }

            return null;
        }

        /// <summary>
        /// Build a dictionary containing the list of parameters for each option
        /// </summary>
        /// <param name="argv"></param>
        /// <returns></returns>
        static Dictionary<string, string[]> CreateOptionDictionary(string[] argv)
        {
            var res = new Dictionary<string, string[]>();
            var currValues = new List<string>();
            var currName = string.Empty;

            foreach (var arg in argv)
            {
                if (arg.StartsWith('-'))
                {
                    saveCurrentOption();
                    currName = arg.TrimStart('-').SplitFirst("=", out string argValue);

                    if (!argValue.IsEmpty())
                    {
                        currValues.Add(argValue);
                        saveCurrentOption();
                    }
                }
                else
                {
                    currValues.Add(arg);
                }
            }
            saveCurrentOption();

            void saveCurrentOption()
            {
                if (!currName.IsEmpty())
                {
                    res[currName] = currValues.ToArray();
                }

                currName = null;
                currValues.Clear();
            }

            return res;
        }
    }
}
