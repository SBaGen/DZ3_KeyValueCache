using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using KeyValueCache.Core;

class Program
{
    static async Task Main(string[] args)
    {
        using var server = new TcpServer(IPAddress.Loopback, 8081);

        // Создаем токен для отмены при нажатии Ctrl+C
        var cts = new CancellationTokenSource();
        
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true; // Отменяем стандартное поведение
            cts.Cancel(); // Отправляем сигнал об остановке
        };

        Console.WriteLine("Сервер запущен. Нажмите Ctrl+C для остановки...");
        
        // Запускаем сервер в фоновой задаче
        var serverTask = Task.Run(async () =>
        {
            try
            {
                await server.StartAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Получен сигнал остановки сервера...");
            }
        });

        // Даем серверу время на инициализацию
        //await Task.Delay(500);

        // Запускаем тестовый клиент
        //await TestClient.RunTestsAsync();

        // Ожидаем завершения работы сервера
        //await serverTask;
    }
}