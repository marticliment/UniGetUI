namespace UniGetUI.Core.Classes.Tests;

public class TaskRecyclerTests
{
    private int MySlowMethod1()
    {
        Thread.Sleep(1000);
        return new Random().Next();
    }

    private class TestClass
    {
        public TestClass() {}

        public string SlowMethod2()
        {
            Thread.Sleep(1000);
            return new Random().Next().ToString();
        }

        public string SlowMethod3()
        {
            Thread.Sleep(1000);
            return new Random().Next().ToString();
        }
    }

    private int MySlowMethod4(int argument)
    {
        Thread.Sleep(1000);
        return new Random().Next() + (argument - argument);
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
    public void TestTaskRecycler_Static_Int_WithCache()
    {
        // The same static method should be cached, and therefore the return value should be the same
        var task1 = TaskRecycler<int>.RunOrAttachAsync(MySlowMethod1, 2);
        var task2 = TaskRecycler<int>.RunOrAttachAsync(MySlowMethod1, 2);
        int result1 = task1.GetAwaiter().GetResult();
        int result2 = task2.GetAwaiter().GetResult();
        Assert.Equal(result1, result2);

        // The same static method should be cached, and therefore the return value should be the same,
        // and equal to previous runs due to 3 seconds cache
        var task3 = TaskRecycler<int>.RunOrAttachAsync(MySlowMethod1, 2);
        var task4 = TaskRecycler<int>.RunOrAttachAsync(MySlowMethod1, 2);
        int result4 = task4.GetAwaiter().GetResult();
        int result3 = task3.GetAwaiter().GetResult();
        Assert.Equal(result3, result4);
        Assert.Equal(result1, result3);

        // Wait for caches to clear
        Thread.Sleep(3000);

        // The same static method should be cached, but cached runs should have been removed. This results should differ from previous ones
        var task5 = TaskRecycler<int>.RunOrAttachAsync(MySlowMethod1, 2);
        var task6 = TaskRecycler<int>.RunOrAttachAsync(MySlowMethod1, 2);
        int result5 = task6.GetAwaiter().GetResult();
        int result6 = task5.GetAwaiter().GetResult();
        Assert.Equal(result5, result6);
        Assert.NotEqual(result4, result5);

        // Clear cache
        TaskRecycler<int>.RemoveFromCache(MySlowMethod1);

        // The same static method should be cached, but cached runs should have been cleared manually. This results should differ from previous ones
        var task7 = TaskRecycler<int>.RunOrAttachAsync(MySlowMethod1, 2);
        var task8 = TaskRecycler<int>.RunOrAttachAsync(MySlowMethod1, 2);
        int result7 = task7.GetAwaiter().GetResult();
        int result8 = task8.GetAwaiter().GetResult();
        Assert.Equal(result7, result8);
        Assert.NotEqual(result6, result7);
    }

    [Fact]
    public void TestTaskRecycler_StaticWithArgument_Int()
    {
        // The same static method should be cached, and therefore the return value should be the same
        var task1 = TaskRecycler<int>.RunOrAttachAsync(MySlowMethod4, 2);
        var task2 = TaskRecycler<int>.RunOrAttachAsync(MySlowMethod4, 2);
        var task3 = TaskRecycler<int>.RunOrAttachAsync(MySlowMethod4, 3);
        int result1 = task1.GetAwaiter().GetResult();
        int result2 = task2.GetAwaiter().GetResult();
        int result3 = task3.GetAwaiter().GetResult();
        Assert.Equal(result1, result2);
        Assert.NotEqual(result1, result3);

        // The same static method should be cached, and therefore the return value should be the same, but different from previous runs
        var task4 = TaskRecycler<int>.RunOrAttachAsync(MySlowMethod4, 2);
        var task5 = TaskRecycler<int>.RunOrAttachAsync(MySlowMethod4, 3);
        int result4 = task4.GetAwaiter().GetResult();
        int result5 = task5.GetAwaiter().GetResult();
        Assert.NotEqual(result4, result5);
        Assert.NotEqual(result1, result4);
        Assert.NotEqual(result3, result5);
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
