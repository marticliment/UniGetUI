namespace UniGetUI.Core.Classes.Tests;

public class TaskRecyclerTests
{
    private int MySlowMethod1()
    {
        Thread.Sleep(1000);
        return (new Random()).Next();
    }

    class TestClass
    {
        public TestClass() {}

        public string SlowMethod2()
        {
            Thread.Sleep(1000);
            return (new Random()).Next().ToString();
        }

        public string SlowMethod3()
        {
            Thread.Sleep(1000);
            return (new Random()).Next().ToString();
        }
    }

    [Fact]
    public void TestTaskRecycler_Static_Int()
    {
        // The same static method should be cached, and therefore the return value should be the same
        var task1 = TaskRecycler<int>.RunOrAttachAsync(MySlowMethod1);
        var task2 = TaskRecycler<int>.RunOrAttachAsync(MySlowMethod1);
        int result1 = task1.GetAwaiter().GetResult();
        int result2 = task2.GetAwaiter().GetResult();
        Assert.Equal(result1, result2);

        // The same static method should be cached, and therefore the return value should be the same, but different from previous runs
        var task3 = TaskRecycler<int>.RunOrAttachAsync(MySlowMethod1);
        var task4 = TaskRecycler<int>.RunOrAttachAsync(MySlowMethod1);
        int result4 = task4.GetAwaiter().GetResult();
        int result3 = task3.GetAwaiter().GetResult();
        Assert.Equal(result3, result4);

        // Ensure the last call was not permanently cached
        Assert.NotEqual(result1, result3);
    }

    [Fact]
    public void TestTaskRecycler_Class_String()
    {
        var class1 = new TestClass();
        var class2 = new TestClass();

        // The SAME method from the SAME instance should be cached,
        // and therefore the return value should be the same
        var task1 = TaskRecycler<string>.RunOrAttachAsync(class1.SlowMethod2);
        var task2 = TaskRecycler<string>.RunOrAttachAsync(class1.SlowMethod2);
        string result1 = task1.GetAwaiter().GetResult();
        string result2 = task2.GetAwaiter().GetResult();
        Assert.Equal(result1, result2);


        var class1_copy = class1;

        // The SAME method from the SAME instance, even when called
        // from different variable names should be cached, and therefore the return value should be the same
        var task5 = TaskRecycler<string>.RunOrAttachAsync(class1_copy.SlowMethod2);
        var task6 = TaskRecycler<string>.RunOrAttachAsync(class1.SlowMethod2);
        string result5 = task5.GetAwaiter().GetResult();
        string result6 = task6.GetAwaiter().GetResult();
        Assert.Equal(result5, result6);

        // The SAME method from two DIFFERENT instances should NOT be
        // cached, so the results should differ
        var task3 = TaskRecycler<string>.RunOrAttachAsync(class1.SlowMethod2);
        var task4 = TaskRecycler<string>.RunOrAttachAsync(class2.SlowMethod2);
        string result4 = task4.GetAwaiter().GetResult();
        string result3 = task3.GetAwaiter().GetResult();
        Assert.NotEqual(result3, result4);

        // Ensure the last call was not permanently cached
        Assert.NotEqual(result1, result3);


        // The SAME method from two DIFFERENT instances should NOT be
        // cached, so the results should differ
        var task7 = TaskRecycler<string>.RunOrAttachAsync(class1.SlowMethod3);
        var task8 = TaskRecycler<string>.RunOrAttachAsync(class2.SlowMethod2);
        string result7 = task7.GetAwaiter().GetResult();
        string result8 = task8.GetAwaiter().GetResult();
        Assert.NotEqual(result7, result8);
    }
}
