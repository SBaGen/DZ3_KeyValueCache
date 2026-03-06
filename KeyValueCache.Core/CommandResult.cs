namespace KeyValueCache.Core;
/// Результат парсинга команды с тремя компонентами: Команда, Ключ и Значение.
/// Использует ReadOnlySpan<char> чтобы избежать лишнего выделения памяти.
public readonly ref struct CommandResult
{
    /// Команда из строки
    public ReadOnlySpan<char> Command { get; }
    
    /// Ключ из строки
    public ReadOnlySpan<char> Key { get; }
    
    /// Значение из строки
    public ReadOnlySpan<char> Value { get; }
    
    /// Создает новый экземпляр структуры CommandResult
    /// <param name="command">Часть с командой</param>
    /// <param name="key">Часть с ключом</param>
    /// <param name="value">Часть со значением</param>
    public CommandResult(ReadOnlySpan<char> command, ReadOnlySpan<char> key, ReadOnlySpan<char> value)
    {
        Command = command;
        Key = key;
        Value = value;
    }
}