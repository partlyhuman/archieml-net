using System;
using ArchieML;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace ArchieML_Tests {
    [TestClass]
    public partial class ScopeTests {
        [TestMethod]
        public void TestNewScope() {
            var result = Archie.Load(@"
{scope}
");
            var expected = JObject.Parse(@"{""scope"": {}}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }

    }
}
