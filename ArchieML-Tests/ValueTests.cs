using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ArchieML;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace ArchieML_Tests {
    /// <summary>
    /// Official unit tests adapted from ArchieML.org.
    /// Series of values*.aml tests.
    /// @see https://github.com/newsdev/archieml.org/tree/gh-pages/test/1.0
    /// </summary>
    [TestClass]
    public partial class ValueTests {
        [TestMethod]
        public void Values01ParsesKeyValuePairs() {
            var result = Archie.Load("key:value");
            var expected = new JObject(new JProperty("key", "value"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }

        [TestMethod]
        public void Values02IgnoresSpacesAroundKey() {
            var result = Archie.Load("  key  :value");
            var expected = new JObject(new JProperty("key", "value"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }

        [TestMethod]
        public void Values03IgnoresTabsAroundKey() {
            var result = Archie.Load("\t\tkey\t:value");
            var expected = new JObject(new JProperty("key", "value"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }

        [TestMethod]
        public void Values04IgnoresSpacesAroundValue() {
            var result = Archie.Load("key:   value   ");
            var expected = new JObject(new JProperty("key", "value"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }

        [TestMethod]
        public void Values05IgnoresTabsAroundValue() {
            var result = Archie.Load("key:\tvalue\t");
            var expected = new JObject(new JProperty("key", "value"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }

        [TestMethod]
        public void Values06DuplicateKeysOverwriteValues() {
            var result = Archie.Load(@"
key:value
key:newvalue");
            var expected = new JObject(new JProperty("key", "newvalue"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }

        [TestMethod]
        public void Values07NonLetterCharactersAtStartOfValue() {
            var result = Archie.Load("key::value");
            var expected = new JObject(new JProperty("key", ":value"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }

        [TestMethod]
        public void Values08KeysAreCaseSensitive() {
            var result = Archie.Load(@"
key: value
Key: Value");
            var expected = new JObject(new JProperty("key", "value"), new JProperty("Key", "Value"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }

        [TestMethod]
        public void Values09LinesWithoutKeysIgnored() {
            var result = Archie.Load(@"
other stuff
key: value
other stuff");
            var expected = new JObject(new JProperty("key", "value"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }

        [TestMethod]
        public void Values10HtmlAllowedInValues() {
            var result = Archie.Load(@"key: <strong>value</strong>");
            var expected = new JObject(new JProperty("key", "<strong>value</strong>"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
    }
}
