using System;
using System.Threading.Tasks;
using Xunit;
using KeyValueCache.Core;

public class SimpleStoreTests
{
    [Fact]
    public async Task MultiThreadedAccess_ShouldBeThreadSafe()
    {
        // Arrange
        using var store = new SimpleStore();
        int threadCount = 10;
        int operationsPerThread = 100;
        
        // Act
        var tasks = new Task[threadCount];
        
        // Половина потоков пишет, половина читает
        for (int i = 0; i < threadCount; i++)
        {
            int threadId = i;
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < operationsPerThread; j++)
                {
                    string key = $"key:{threadId}:{j}";
                    byte[] value = System.Text.Encoding.UTF8.GetBytes($"value:{j}");
                    
                    if (threadId % 2 == 0)
                    {
                        store.Set(key, value);
                    }
                    else
                    {
                        store.Get(key);
                    }
                }
            });
        }
        
        await Task.WhenAll(tasks);
        
        // Assert
        var stats = store.GetStatistics();
        Assert.Equal(5 * operationsPerThread, stats.setCount);   // 5 потоков писали
        Assert.Equal(5 * operationsPerThread, stats.getCount);   // 5 потоков читали
        Assert.Equal(0, stats.deleteCount);
    }

    [Fact]
    public void SetAndGet_ShouldReturnCorrectValue()
    {
        // Arrange
        using var store = new SimpleStore();
        var expectedValue = System.Text.Encoding.UTF8.GetBytes("hello");
        
        // Act
        store.Set("test:key", expectedValue);
        var actualValue = store.Get("test:key");
        
        // Assert
        Assert.NotNull(actualValue);
        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public void Delete_ShouldRemoveKey()
    {
        // Arrange
        using var store = new SimpleStore();
        store.Set("test:key", System.Text.Encoding.UTF8.GetBytes("value"));
        
        // Act
        store.Delete("test:key");
        var result = store.Get("test:key");
        
        // Assert
        Assert.Null(result);
        
        var stats = store.GetStatistics();
        Assert.Equal(1, stats.deleteCount);
    }
}