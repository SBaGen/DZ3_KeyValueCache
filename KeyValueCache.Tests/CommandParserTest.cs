using KeyValueCache.Core;
using Xunit;

namespace KeyValueCache.Tests;

public class CommandParserTest
{
    [Fact]
    public void Parse_SetCommandWithThreeArguments_ReturnsCorrectCommandResult()
    {
        // Arrange
        string input = "SET key1 value1";
        
        // Act
        var result = CommandParser.Parse(input.AsSpan());
        
        // Assert
        Assert.Equal("SET", result.Command.ToString());
        Assert.Equal("key1", result.Key.ToString());
        Assert.Equal("value1", result.Value.ToString());
    }
    
    [Fact]
    public void Parse_GetCommandWithTwoArguments_ReturnsCorrectCommandResult()
    {
        // Arrange
        string input = "GET key1";
        
        // Act
        var result = CommandParser.Parse(input.AsSpan());
        
        // Assert
        Assert.Equal("GET", result.Command.ToString());
        Assert.Equal("key1", result.Key.ToString());
        Assert.True(result.Value.IsEmpty);
    }
    
    [Fact]
    public void Parse_DeleteCommandWithTwoArguments_ReturnsCorrectCommandResult()
    {
        // Arrange
        string input = "DELETE key1";
        
        // Act
        var result = CommandParser.Parse(input.AsSpan());
        
        // Assert
        Assert.Equal("DELETE", result.Command.ToString());
        Assert.Equal("key1", result.Key.ToString());
        Assert.True(result.Value.IsEmpty);
    }
    
    [Fact]
    public void Parse_InvalidCommandWithoutKey_ReturnsDefault()
    {
        // Arrange 
        string input = "GET"; // Команда без ключа
        
        // Act
        var result = CommandParser.Parse(input.AsSpan());
        
        // Assert
        Assert.True(result.Command.IsEmpty);
        Assert.True(result.Key.IsEmpty);
        Assert.True(result.Value.IsEmpty);
    }
    
    [Fact]
    public void Parse_CommandWithExtraSpaces_ReturnsCorrectCommandResult()
    {
        // Arrange
        string input = "  SET   key1   value1   "; // Команда с лишними пробелами
        
        // Act
        var result = CommandParser.Parse(input.AsSpan());
        
        // Assert
        Assert.Equal("SET", result.Command.ToString());
        Assert.Equal("key1", result.Key.ToString());
        Assert.Equal("value1", result.Value.ToString());
    }
    
    [Fact]
    public void Parse_EmptyString_ReturnsDefault()
    {
        // Arrange
        string input = "";
        
        // Act
        var result = CommandParser.Parse(input.AsSpan());
        
        // Assert
        Assert.True(result.Command.IsEmpty);
        Assert.True(result.Key.IsEmpty);
        Assert.True(result.Value.IsEmpty);
    }
    
    [Fact]
    public void Parse_WhitespaceOnly_ReturnsDefault()
    {
        // Arrange
        string input = "   ";
        
        // Act
        var result = CommandParser.Parse(input.AsSpan());
        
        // Assert
        Assert.True(result.Command.IsEmpty);
        Assert.True(result.Key.IsEmpty);
        Assert.True(result.Value.IsEmpty);
    }
    
    [Fact]
    public void Parse_InvalidCommand_ReturnsDefault()
    {
        // Arrange
        string input = "INVALID key1";
        
        // Act
        var result = CommandParser.Parse(input.AsSpan());
        
        // Assert
        Assert.True(result.Command.IsEmpty);
        Assert.True(result.Key.IsEmpty);
        Assert.True(result.Value.IsEmpty);
    }
    
    [Fact]
    public void Parse_SetCommandWithoutValue_ReturnsDefault()
    {
        // Arrange
        string input = "SET key1";
        
        // Act
        var result = CommandParser.Parse(input.AsSpan());
        
        // Assert
        Assert.True(result.Command.IsEmpty);
        Assert.True(result.Key.IsEmpty);
        Assert.True(result.Value.IsEmpty);
    }
    
    [Fact]
    public void Parse_CommandCaseInsensitive()
    {
        // Arrange
        string input = "set key1 value1";
        
        // Act
        var result = CommandParser.Parse(input.AsSpan());
        
        // Assert
        Assert.Equal("set", result.Command.ToString()); // Команда в нижнем регистре
        Assert.Equal("key1", result.Key.ToString());
        Assert.Equal("value1", result.Value.ToString());
    }
}