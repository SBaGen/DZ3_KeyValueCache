using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

// Класс тестового клиента для подключения к серверу KeyValueCache
// Не является точкой входа в приложение
class TestClient
{
    // Метод для выполнения тестирования сервера
    public static async Task RunTestsAsync()
    {
        using var client = new TcpClient();
        
        try
        {
            await client.ConnectAsync(IPAddress.Loopback, 8081);
            Console.WriteLine("Подключено к серверу");

            var stream = client.GetStream();
            
            // Отправляем тестовые команды
            string[] testCommands = {
                "SET key1 value1\n",
                "GET key1\n",
                "SET user John\n",
                "GET user\n",
                "DELETE key1\n",
                "GET key1\n",
                ""
                };
            
            foreach (string command in testCommands)
            {
                byte[] data = Encoding.UTF8.GetBytes(command);
                await stream.WriteAsync(data, 0, data.Length);
                Console.WriteLine($"Отправлено: {command.Trim()}");
                
                await Task.Delay(100); // Небольшая задержка между командами
            }
            
            await Task.Delay(1000); // Ждем ответы от сервера
            
            Console.WriteLine("Тестирование завершено");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка клиента: {ex.Message}");
        }
    }
}