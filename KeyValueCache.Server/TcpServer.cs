using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KeyValueCache.Core;

public sealed class TcpServer : IDisposable
{
    private readonly Socket _serverSocket;
    private readonly IPEndPoint _endPoint;
    private readonly CancellationTokenSource _cts = new();

    public TcpServer(IPAddress address, int port)
    {
        _endPoint = new IPEndPoint(address, port);
        _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    }
    /// <summary>
    /// Запускает сервер, который принимает входящие подключения и 
    /// обрабатывает их в фоновом режиме.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _serverSocket.Bind(_endPoint);
        _serverSocket.Listen(10); // backlog
        Console.WriteLine($"Сервер запущен на {_endPoint}...");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var clientSocket = await _serverSocket.AcceptAsync(cancellationToken);
                Console.WriteLine($"Принято новое подключение от {clientSocket.RemoteEndPoint}");
                
                // Запуск обработки клиента в фоне. Исключения НЕ должны подниматься выше.
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessClientAsync(clientSocket, _cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Ожидаемое исключение при отмене операции
                        Console.WriteLine($"Обработка клиента {clientSocket.RemoteEndPoint} прервана по запросу отмены.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Неожиданная ошибка при обработке клиента {clientSocket.RemoteEndPoint}: {ex}");
                    }
                }, _cts.Token);
            }
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken || ex.CancellationToken == _cts.Token)
        {
            // Ожидаемое исключение при отмене операции сервера
            Console.WriteLine($"Сервер остановлен по запросу отмены. Token: {(ex.CancellationToken == cancellationToken ? "external" : "internal")}");
        }
        catch (ObjectDisposedException)
        {
            // Возникает, когда сокет был закрыт во время ожидания AcceptAsync
            Console.WriteLine("Сервер остановлен: сокет был закрыт.");
        }
        finally
        {
            Dispose();
        }
    }
    /// <summary>
    /// Обрабатывает подключение клиента, читая данные в цикле до тех пор, 
    /// пока не будет получен сигнал отмены или клиент не отключится.
    /// </summary>
    /// <param name="clientSocket"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async Task ProcessClientAsync(Socket clientSocket, CancellationToken ct)
    {
        const int bufferSize = 4096;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        int totalBytesInBuffer = 0; // Сколько байт реально в буфере

        try
        {
            while (!ct.IsCancellationRequested)
            {
                // Читаем данные в свободное пространство буфера
                int bytesReceived = await clientSocket.ReceiveAsync(
                    new Memory<byte>(buffer, totalBytesInBuffer, bufferSize - totalBytesInBuffer),
                    SocketFlags.None,
                    ct);

                if (bytesReceived == 0)
                {
                    // Клиент отключился
                    break;
                }

                // Увеличиваем счётчик данных в буфере
                totalBytesInBuffer += bytesReceived;

                // Обрабатываем все полные команды в буфере
                totalBytesInBuffer = ProcessBuffer(buffer, totalBytesInBuffer, clientSocket);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            
            try
            {
                clientSocket.Shutdown(SocketShutdown.Both);
            }
            catch { }
            
            clientSocket.Dispose();
            Console.WriteLine($"Клиент {clientSocket.RemoteEndPoint} отключен");
        }
    }

    /// <summary>
    /// Обрабатывает буфер, извлекая полные команды (разделённые \n)
    /// </summary>
    private int ProcessBuffer(byte[] buffer, int totalBytes, Socket clientSocket)
    {

        int processedUpTo = 0;
    
        while (processedUpTo < totalBytes)
        {
            // Ищем символ новой строки, начиная с текущей позиции
            int newlinePos = Array.IndexOf(buffer, (byte)'\n', processedUpTo, totalBytes - processedUpTo);
            
            // Если нет полной команды (нет \n), выходим
            if (newlinePos == -1)
            {
                break;
            }

            // Определяем длину команды (без \n)
            int commandLength = newlinePos - processedUpTo;
            
            // Если есть \r перед \n (Windows-стиль), убираем его тоже
            if (commandLength > 0 && buffer[newlinePos - 1] == '\r')
            {
                commandLength--;
            }


            // Парсим команду
            string commandLine = Encoding.UTF8.GetString(buffer, processedUpTo, commandLength);
            var parsedCommand = CommandParser.Parse(commandLine.AsSpan());

            // Преобразование ReadOnlySpan в строки для вывода
            string commandStr = parsedCommand.Command.IsEmpty ? "" : parsedCommand.Command.ToString();
            string keyStr = parsedCommand.Key.IsEmpty ? "" : parsedCommand.Key.ToString();
            string valueStr = parsedCommand.Value.IsEmpty ? "" : parsedCommand.Value.ToString();
            
            Console.WriteLine($"[{clientSocket.RemoteEndPoint}] Получена команда: {commandStr}, Ключ: {keyStr}, Значение: {valueStr}");


            // Переходим к следующей команде
            processedUpTo = newlinePos + 1;
        }

        // Сдвигаем оставшиеся данные в начало буфера
        if (processedUpTo > 0 && processedUpTo < totalBytes)
        {
            int remainingBytes = totalBytes - processedUpTo;
            Buffer.BlockCopy(buffer, processedUpTo, buffer, 0, remainingBytes);
            return remainingBytes;
        }
        // Если всё обработано
        else if (processedUpTo >= totalBytes)
        {
            return 0;
        }
        // Если ничего не обработано (неполная команда)
        else
        {
            return totalBytes;
        }
    }    
    private int _disposed = 0;
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    private void Dispose(bool disposing)
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
            return;

        if (disposing)
        {
            try
            {
                _cts?.Cancel();
                // Сначала закрываем сокет, чтобы прервать все текущие операции AcceptAsync и ReceiveAsync
                try
                {
                    _serverSocket?.Close();
                }
                catch (ObjectDisposedException)
                {
                    // игнорируем, если сокет уже был закрыт
                }
                
                // Затем освобождаем сокет
                try
                {
                    _serverSocket?.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // игнорируем, если сокет уже был закрыт
                }
                
                // освобождаем CancellationTokenSource
                try
                {
                    _cts?.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // игнорируем, если уже был освобожден
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при освобождении ресурсов сервера: {ex.Message}");
            }
        }
        
        Console.WriteLine("Сервер остановлен.");
    }
}