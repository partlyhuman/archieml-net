using Microsoft.VisualStudio.TestTools.UnitTesting;
using ArchieML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchieML.Tests {
    [TestClass]
    public partial class ArchieMLTests {
        [TestMethod]
        public void TestRunWithoutThrowingException() {
            //Try loading an empty string
            Archie.Load("");
            //If you don't throw an exception, great!
        }

        [TestMethod]
        public void TestBasicKeys() {
            var result = Archie.Load(@"
This is a key:
  key: value
It's a nice key!");
            Assert.Equals(result, new Dictionary<string, object>() {
                {"key", "value" }
            });
        }
    }
}