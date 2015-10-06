using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ArchieML;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArchieML_Tests {
    /// <summary>
    /// Official unit tests adapted from ArchieML.org.
    /// Series of values*.aml tests.
    /// @see https://github.com/newsdev/archieml.org/tree/gh-pages/test/1.0
    /// </summary>
    [TestClass]
    public partial class ValueTests {
        [TestMethod]
        public void TestParsesKeyValuePairs() {
            var result = Archie.Load("key:value");
            var expected = new Hashtable() { { "key", "value" } };
            //var expected = new Dictionary<string, object> { { "key", "value" } };
            CollectionAssert.AreEquivalent(expected, (ICollection)result);
        }

        [TestMethod]
        public void TestIgnoresSpacesAroundKey() {
            var result = Archie.Load("  key  :value");
            var expected = new Hashtable() { { "key", "value" } };
            CollectionAssert.AreEquivalent(expected, (ICollection)result);
        }

        [TestMethod]
        public void TestIgnoresTabsAroundKey() {
            var result = Archie.Load("\t\tkey\t:value");
            var expected = new Hashtable() { { "key", "value" } };
            CollectionAssert.AreEquivalent(expected, (ICollection)result);
        }

        [TestMethod]
        public void TestIgnoresSpacesAroundValue() {
            var result = Archie.Load("key:   value   ");
            var expected = new Hashtable() { { "key", "value" } };
            CollectionAssert.AreEquivalent(expected, (ICollection)result);
        }

        [TestMethod]
        public void TestIgnoresTabsAroundValue() {
            var result = Archie.Load("key:\tvalue\t");
            var expected = new Hashtable() { { "key", "value" } };
            CollectionAssert.AreEquivalent(expected, (ICollection)result);
        }

        [TestMethod]
        public void TestDuplicateKeysOverwriteValues() {
            var result = Archie.Load(@"
key:value
key:newvalue");
            var expected = new Hashtable() { { "key", "newvalue" } };
            CollectionAssert.AreEquivalent(expected, (ICollection)result);
        }

        [TestMethod]
        public void TestNonLetterCharactersAtStartOfValue() {
            var result = Archie.Load("key::value");
            var expected = new Hashtable() { { "key", "value" } };
            CollectionAssert.AreEquivalent(expected, (ICollection)result);
        }

        [TestMethod]
        public void TestKeysAreCaseSensitive() {
            var result = Archie.Load(@"
key: value
Key: Value");
            var expected = new Hashtable() { { "key", "value" }, { "Key", "Value"} };
            CollectionAssert.AreEquivalent(expected, (ICollection)result);
        }

        [TestMethod]
        public void TestLinesWithoutKeysIgnored() {
            var result = Archie.Load(@"
other stuff
key: value
other stuff");
            var expected = new Hashtable() { { "key", "value" } };
            CollectionAssert.AreEquivalent(expected, (ICollection)result);
        }

        [TestMethod]
        public void TestHtmlAllowedInValues() {
            var result = Archie.Load(@"key: <strong>value</strong>");
            var expected = new Hashtable() { { "key", "<strong>value</strong>" } };
            CollectionAssert.AreEquivalent(expected, (ICollection)result);
        }
    }
}
