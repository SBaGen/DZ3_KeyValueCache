namespace KeyValueCache.Core;

/// Парсер команд с ReadOnlySpan<char>.
/// Разбирает строки в формате "COMMAND KEY VALUE", где команда, ключ и значение разделены пробелами.
public static class CommandParser
{
    /// Разбирает строку команды на компоненты: Команда, Ключ и Значение.
    /// Проверяет правильность команды (SET/GET/DELETE) и наличие необходимых параметров.
    /// <param name="input">Входная строка для разбора</param>
    /// <returns>CommandResult (структура с полями Команда,Ключ,Значение) или пустую структуру при ошибке</returns>
    public static CommandResult Parse(ReadOnlySpan<char> input)
    {
        // Проверяем, пустая ли строка
        if (input.IsEmpty)
        {
            return default;
        }

        // Убираем ведущие и завершающие пробелы
        input = input.Trim();

        // Если строка стала пустой после обрезки, возвращаем дефолт
        if (input.IsEmpty)
        {
            return default;
        }

        // Находим первый пробел, чтобы отделить команду от остального
        int firstSpaceIndex = input.IndexOf(' ');

        ReadOnlySpan<char> command;
        ReadOnlySpan<char> remainder = ReadOnlySpan<char>.Empty;

        if (firstSpaceIndex == -1)
        {
            // Пробелов нет, считаем всю строку командой
            command = input;
        }
        else
        {
            command = input.Slice(0, firstSpaceIndex);
            remainder = input.Slice(firstSpaceIndex + 1).Trim();
        }

        // Проверяем, что команда не пустая
        if (command.IsEmpty)
        {
            return default;
        }

        // Сравнения без аллокаций
        static bool EqualsIgnoreAsciiCase(ReadOnlySpan<char> span, string cmp)
        {
            if (span.Length != cmp.Length) return false;
            for (int i = 0; i < span.Length; i++)
            {
                if (char.ToUpperInvariant(span[i]) != char.ToUpperInvariant(cmp[i])) return false;
            }
            return true;
        }

        // Проверяем допустимые команды
        if (!EqualsIgnoreAsciiCase(command, "SET") &&
            !EqualsIgnoreAsciiCase(command, "GET") &&
            !EqualsIgnoreAsciiCase(command, "DELETE"))
        {
            return default;
        }

        // Обработка команды SET (требует ключ и значение)
        if (EqualsIgnoreAsciiCase(command, "SET"))
        {
            if (remainder.IsEmpty) return default;

            int secondSpaceIndex = remainder.IndexOf(' ');

            if (secondSpaceIndex == -1)
            {
                // Только ключ, нет значения
                return default;
            }

            ReadOnlySpan<char> key = remainder.Slice(0, secondSpaceIndex).Trim();
            ReadOnlySpan<char> value = remainder.Slice(secondSpaceIndex + 1).Trim();

            if (key.IsEmpty) return default;
            if (value.IsEmpty) return default;

            return new CommandResult(command, key, value);
        }
        // Обработка команды GET (требует только ключ)
        else if (EqualsIgnoreAsciiCase(command, "GET"))
        {
            if (remainder.IsEmpty) return default;

            ReadOnlySpan<char> key = remainder.Trim();
            if (key.IsEmpty) return default;

            return new CommandResult(command, key, ReadOnlySpan<char>.Empty);
        }
        // Обработка команды DELETE (требует только ключ)
        else if (EqualsIgnoreAsciiCase(command, "DELETE"))
        {
            if (remainder.IsEmpty) return default;

            ReadOnlySpan<char> key = remainder.Trim();
            if (key.IsEmpty) return default;

            return new CommandResult(command, key, ReadOnlySpan<char>.Empty);
        }

        // какая-то ошибка
        return default;
    }

}