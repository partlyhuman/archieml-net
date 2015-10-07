using System;
using ArchieML;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace ArchieML_Tests {
    [TestClass]
    public class IgnoreTests {
        [TestMethod]
        public void Ignore01TextBeforeIgnoreCommandIncluded() {
            var result = Archie.Load(@"
key:value
:ignore
");
            var expected = new JObject(new JProperty("key", "value"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Ignore02TextAfterIgnoreCommandIgnored() {
            var result = Archie.Load(@"
:ignore
key:value
");
            var expected = new JObject();
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Ignore03IgnoreCommandCaseInsensitive() {
            var result = Archie.Load(@"
:iGnOrE
key:value
");
            var expected = new JObject();
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Ignore04SpacesSurroundingIgnoreCommandAllowed() {
            var result = Archie.Load(@"
  :ignore  
key:value
");
            var expected = new JObject();
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Ignore05TabsSurroundingIgnoreCommandAllowed() {
            var result = Archie.Load(@"
		:ignore		
key:value
");
            var expected = new JObject();
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Ignore06ParseIgnoreCommandWithAdditionalCharacters() {
            var result = Archie.Load(@"
:ignorethis
key:value
");
            var expected = new JObject();
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Ignore07ParseIgnoreCommandWithAdditionalWhitespaceAndCharacters() {
            var result = Archie.Load(@"
:ignore the below
key:value
");
            var expected = new JObject();
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Ignore08ParseIgnoreCommandWithTabAndAdditionalCharacters() {
            var result = Archie.Load(@"
:ignore	the below
key:value
");
            var expected = new JObject();
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
    }
}
