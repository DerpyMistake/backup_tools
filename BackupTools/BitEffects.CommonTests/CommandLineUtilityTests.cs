using NUnit.Framework;
using BitEffects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitEffects.Tests
{
    [TestFixture]
    public class CommandLineUtilityTests
    {
        class OptionsWithFields
        {
            public string strField = null;
            public int intField = 0;
            private string privateField = null;

            public string GetPrivateField() => privateField;
        }

        class OptionsWithProperties
        {
            public string StrProp { get; set; }
            public int IntProp { get; set; }
            public string[] ArrStrProp { get; set; }
            public int[] ArrIntProp { get; set; }

            public bool BoolProp { get; set; }
            public bool TrueBoolProp { get; set; } = true;

            public string GetterProp { get; }
            private string PrivateProp { get; set; }

            public string GetPrivateProperty() => PrivateProp;
        }

        class OptionsWithAliases
        {
            public string dashedProp { get; set; }
            public string ABBRProp { get; set; }

            [Alias("AP")]
            public string aliasedProp { get; set; }
        }

        class OptionsWithConverter : IConvertOptions
        {
            public string StrProp { get; set; }
            public string ConvProp { get; set; }
            public string DashedProp { get; set; }

            public bool Convert(string optionName, string[] optionValues)
            {
                if (optionName.Equals(nameof(ConvProp)))
                {
                    ConvProp = nameof(ConvProp);
                    return true;
                }
                if (optionName.Equals(nameof(DashedProp)))
                {
                    DashedProp = nameof(DashedProp);
                    return true;
                }

                return false;
            }
        }

        [Test]
        public void CanParseBooleanProperties()
        {
            string[] args =
            {
                "ignored", "ignored",
                "-boolProp", "-strProp",
                "--trueBoolProp=false"
            };
            OptionsWithProperties options = null;

            Assert.DoesNotThrow(() =>
            {
                options = CommandLineUtility.ParseOptions<OptionsWithProperties>(args);
            });

            Assert.AreEqual(string.Empty, options.StrProp);
            Assert.AreEqual(true, options.BoolProp);
            Assert.AreEqual(false, options.TrueBoolProp);
        }

        [Test]
        public void CanParseOptionsWithFields()
        {
            string[] args =
            {
                "ignored", "ignored",
                "-strField", "1",
                "--intField", "2"
            };
            OptionsWithFields options = null;

            Assert.DoesNotThrow(() =>
            {
                options = CommandLineUtility.ParseOptions<OptionsWithFields>(args);
            });

            Assert.AreEqual("1", options.strField);
            Assert.AreEqual(2, options.intField);
        }

        [Test]
        public void PrivateFieldsAreNotSet()
        {
            string[] args =
            {
                "ignored", "ignored",
                "--privateField", "1"
            };
            OptionsWithFields options = null;

            Assert.DoesNotThrow(() =>
            {
                options = CommandLineUtility.ParseOptions<OptionsWithFields>(args);
            });

            Assert.IsNull(options.GetPrivateField());
        }

        [Test]
        public void CanParseOptionsWithProperties()
        {
            string[] args =
            {
                "ignored", "ignored",
                "-strProp", "1",
                "--intProp", "2"
            };
            OptionsWithProperties options = null;

            Assert.DoesNotThrow(() =>
            {
                options = CommandLineUtility.ParseOptions<OptionsWithProperties>(args);
            });

            Assert.AreEqual("1", options.StrProp);
            Assert.AreEqual(2, options.IntProp);
        }

        [Test]
        public void PrivatePropertiesAreNotSet()
        {
            string[] args =
            {
                "ignored", "ignored",
                "--getterProp", "1",
                "--privateProp", "2"
            };
            OptionsWithProperties options = null;

            Assert.DoesNotThrow(() =>
            {
                options = CommandLineUtility.ParseOptions<OptionsWithProperties>(args);
            });

            Assert.IsNull(options.GetterProp);
            Assert.IsNull(options.GetPrivateProperty());
        }

        [Test]
        public void CanParseOptionsWithAliases()
        {
            string[] args =
            {
                "ignored", "ignored",
                "-AP", "1"
            };
            OptionsWithAliases options = null;

            Assert.DoesNotThrow(() =>
            {
                options = CommandLineUtility.ParseOptions<OptionsWithAliases>(args);
            });

            Assert.AreEqual("1", options.aliasedProp);
        }

        [Test]
        public void CanParseOptionsWithDashes()
        {
            string[] args =
            {
                "ignored", "ignored",
                "--dashed-prop", "1",
                "--abbr-prop", "2"
            };
            OptionsWithAliases options = null;

            Assert.DoesNotThrow(() =>
            {
                options = CommandLineUtility.ParseOptions<OptionsWithAliases>(args);
            });

            Assert.AreEqual("1", options.dashedProp);
            Assert.AreEqual("2", options.ABBRProp);
        }

        [Test]
        public void OptionsCanConvertValues()
        {
            string[] args =
            {
                "ignored", "ignored",
                "--strProp", "1",
                "--convProp", "2",
                "--dashed-prop", "3"
            };
            OptionsWithConverter options = null;

            Assert.DoesNotThrow(() =>
            {
                options = CommandLineUtility.ParseOptions<OptionsWithConverter>(args);
            });

            Assert.AreEqual("1", options.StrProp);
            Assert.AreEqual(nameof(options.ConvProp), options.ConvProp);
            Assert.AreEqual(nameof(options.DashedProp), options.DashedProp);
        }

        [Test]
        public void CanParseStringValues()
        {
            string[] args =
            {
                "ignored", "ignored",
                "--strProp", "a", "long string"
            };
            OptionsWithProperties options = null;

            Assert.DoesNotThrow(() =>
            {
                options = CommandLineUtility.ParseOptions<OptionsWithProperties>(args);
            });

            Assert.AreEqual("a long string", options.StrProp);
        }

        [Test]
        public void CanParseArrayValues()
        {
            string[] args =
            {
                "ignored", "ignored",
                "--arrStrProp", "a", "b",
                "--arrIntProp", "1", "2"
            };
            OptionsWithProperties options = null;

            Assert.DoesNotThrow(() =>
            {
                options = CommandLineUtility.ParseOptions<OptionsWithProperties>(args);
            });

            Assert.AreEqual(options.ArrStrProp?.Length, 2);
            Assert.AreEqual(options.ArrIntProp?.Length, 2);
            Assert.AreEqual("a", options.ArrStrProp[0]);
            Assert.AreEqual("b", options.ArrStrProp[1]);
            Assert.AreEqual(1, options.ArrIntProp[0]);
            Assert.AreEqual(2, options.ArrIntProp[1]);
        }
    }
}