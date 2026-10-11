using Microsoft.Azure.Cosmos;
using Persistence;
using System.Security.Cryptography;
using System.Text;

public class Program
{
    public static async Task Main()
    {
        try
        {
            IStorage storage =
                await StorageFactory.CreateAsync(ModuleType.Chat);

            // test 1
            string key = "message-011";

            await storage.Create(key, Encoding.UTF8.GetBytes("Hello from Chat1"));
            Console.WriteLine("Item created.");

            Console.WriteLine($"Read: {Encoding.UTF8.GetString(await storage.Read(key))}");

            await storage.Update(key, Encoding.UTF8.GetBytes("Updated message"));
            Console.WriteLine("Item updated.");

            byte[] bytes = await storage.Read(key);
            Console.WriteLine(
                $"Read bytes after update: {string.Join(", ", bytes)}");
            
            Console.WriteLine(
                $"Read data after update: {Encoding.UTF8.GetString(await storage.Read(key))}");


            await storage.Delete(key);
            Console.WriteLine("Item deleted.");

            // test 2
            string key2 = "message-018";

            await storage.Create(key2, Encoding.UTF8.GetBytes("Hello from Chat2"));

            await storage.Update(key2, Encoding.UTF8.GetBytes("Updated message from Chat2"));
            byte[] data2 = await storage.Read(key2);
            Console.WriteLine($"Read data from key2: {Encoding.UTF8.GetString(data2)}");

            await storage.Delete(key2);
        }
        catch (CosmosException ex)
        {
            Console.WriteLine($"Cosmos error : {ex.Message}");
        }


    }
}