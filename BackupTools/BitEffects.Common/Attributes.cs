using System;

namespace BitEffects
{
    /// <summary>
    /// Add a note to a specific element, which can be later used in code analysis
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public class RemarkAttribute : Attribute
    {
        public string Description { get; }

        public RemarkAttribute(string description)
        {
            this.Description = description;
        }
    }

    /// <summary>
    /// Specify a limitation for a feature, which can be later used in code analysis
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public class CaveatAttribute : Attribute
    {
        public string Description { get; }

        public CaveatAttribute(string description)
        {
            this.Description = description;
        }
    }
}
