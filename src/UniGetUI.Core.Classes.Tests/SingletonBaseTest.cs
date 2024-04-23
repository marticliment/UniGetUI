using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniGetUI.Core.Classes.Tests
{
    [TestClass]
    public class SingletonBaseTest
    {

        private class InheritedClass1 : SingletonBase<InheritedClass1>
        {
            public int Attribute1 { get; set; } = 0;
        }

        private class InheritedClass2 : SingletonBase<InheritedClass2>
        {
            public int Attribute1 { get; set; } = 0;
        }


        [TestMethod]
        public void TestSingletonClass()
        {
            var Type1Instance1 = InheritedClass1.Instance;
            Type1Instance1.Attribute1 = 1;

            var Type1Instance2 = InheritedClass1.Instance;
            Type1Instance2.Attribute1 = 3;

            Assert.AreEqual(Type1Instance1.Attribute1, Type1Instance2.Attribute1, "The instances of the class have attributes with different values");
            Assert.AreEqual(Type1Instance1, Type1Instance2, "The instances are different");

            var Type2Instance1 = new InheritedClass2();
            Type2Instance1.Attribute1 = 2;

            Assert.AreNotEqual(Type1Instance1.Attribute1, Type2Instance1.Attribute1, "The instances of different singleton types have shared attribute values");
        }
    }
}
