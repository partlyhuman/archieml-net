using System;
using ArchieML;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace ArchieML_Tests {
    [TestClass]
    public class NestedArrayTests {
        [TestMethod]
        public void ArraysNested01ArraysBeginningWithDotsCreateComplexSubarrays() {
            var result = Archie.Load(@"
[array]
[.subarray]
key: value
");
            var expected = JObject.Parse(@"{'array': [{'subarray': [{'key': 'value'}]}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysNested02ArrayKeysBeginningWithDotsCreateSimpleSubarrays() {
            var result = Archie.Load(@"
[array]
[.subarray]
* Value 1
* Value 2
");
            var expected = JObject.Parse(@"{'array': [{'subarray': ['Value 1', 'Value 2']}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }

    }
}
