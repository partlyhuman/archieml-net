using System;
using ArchieML;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace ArchieML_Tests {
    [TestClass]
    public class ArrayTests {
        [TestMethod]
        public void Arrays01ArrayCommandCreatesEmptyArray() {
            var result = Archie.Load(@"[array]");
            var expected = JObject.Parse(@"{'array': []}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Arrays02IgnoresSpacesAroundArrayCommand() {
            var result = Archie.Load(@"  [array]  ");
            var expected = JObject.Parse(@"{'array': []}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Arrays03IgnoresTabsAroundArrayCommand() {
            var result = Archie.Load(@"     [array]     ");
            var expected = JObject.Parse(@"{'array': []}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Arrays04IgnoresSpacesAroundArrayName() {
            var result = Archie.Load(@"[  array  ]");
            var expected = JObject.Parse(@"{'array': []}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Arrays05IgnoresTabsAroundArrayName() {
            var result = Archie.Load(@"[        array     ]");
            var expected = JObject.Parse(@"{'array': []}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Arrays06IgnoresTextAfterArrayCommand() {
            var result = Archie.Load(@"[array]a");
            var expected = JObject.Parse(@"{'array': []}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Arrays07ArraysCanBeNestedUsingDotNotation() {
            var result = Archie.Load(@"[scope.array]");
            var expected = JObject.Parse(@"{'scope': {'array': []}}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Arrays08ArrayValuesCanBeNestedUsingDotNotation() {
            var result = Archie.Load(@"
[array]
scope.key: value
scope.key: value
");
            var expected = JObject.Parse(@"{'array': [{'scope': {'key': 'value'}}, {'scope': {'key': 'value'}}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }


    }
}
