using Xunit;

namespace UniGetUI.Core.Classes.Tests
{
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


        [Fact]
        public void TestSingletonClass()
        {
            var Type1Instance1 = InheritedClass1.Instance;
            Type1Instance1.Attribute1 = 1;

            var Type1Instance2 = InheritedClass1.Instance;
            Type1Instance2.Attribute1 = 3;

            Assert.Equal(Type1Instance1.Attribute1, Type1Instance2.Attribute1);
            Assert.Equal(Type1Instance1, Type1Instance2);

            var Type2Instance1 = new InheritedClass2();
            Type2Instance1.Attribute1 = 2;

            Assert.NotEqual(Type1Instance1.Attribute1, Type2Instance1.Attribute1);
        }
    }
}
