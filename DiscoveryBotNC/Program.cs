using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DiscoveryBotNC
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //Середовище в якому виконується програма
            string env = Environment.GetEnvironmentVariable("DiscoveryBotNC_Environment");

            //Завантажуємо всі джерела конфігурації
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder
                .AddJsonFile("appSettings.json", true)
                .AddJsonFile($"appSettings.{env}.json", true)
                .AddEnvironmentVariables();

            //Створюємо конфігурацію
            var config = configurationBuilder.Build();

            //Витягуємо LogLevel з конфігурації
            LogSeverity logLevel = LogSeverity.Info;
            string logLevelString = config.GetSection("Logging")["LogLevel"];
            if (logLevelString == null)
                await Log(new LogMessage(LogSeverity.Warning, "Main", "Logging.LogLevel is not specified, used \"Info\""));
            else 
                try
                {
                    logLevel = Enum.Parse<LogSeverity>(logLevelString);
                }
                catch (Exception e)
                {
                    await Log(new LogMessage(LogSeverity.Warning, "Main", $"Unnable to convert \"{logLevelString}\"(Logging.LogLevel) to valid LogSeverity , used \"Info\"", e));
                }
            

            //Створюємо клієнт Discord
            var client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = logLevel
            });
            client.Log += Log;
            string token = new ConfigurationBuilder().AddJsonFile("secrets.json").Build().GetSection("Token").Value;
            await client.LoginAsync(TokenType.Bot,token);
            await client.StartAsync();
            await Task.Delay(-1);
        }

        //Об'єкт для блокування інших потоків
        private static object _consoleLocker = new object();
        private static Task Log(LogMessage arg)
        {
            //блокування інших потоків
            lock (_consoleLocker) 
            {
                //В залежності від LogLevel по різному відображуємо інформацію
                switch (arg.Severity) 
                {
                    case LogSeverity.Debug:
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.WriteLine($"[Deb ]\t{arg.Source}: {arg.Message}");
                        break;
                    case LogSeverity.Verbose:
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.WriteLine($"[Verb]\t{arg.Source}: {arg.Message}");
                        break;
                    case LogSeverity.Info:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.WriteLine($"[Info]\t{arg.Source}: {arg.Message}");
                        break;
                    case LogSeverity.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.WriteLine($"[Warn]\t{arg.Source}: {arg.Message}");
                        break;
                    case LogSeverity.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.WriteLine($"[Err ]\t{arg.Source}: {arg.Message}");
                        break;
                    case LogSeverity.Critical:
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.Write("[Crit]");
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.WriteLine($"\t{arg.Source}: {arg.Message}");
                        break;
                }

                //Логуємо екзепшин, якщо є
                if (arg.Exception != null) 
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.Write("[Excp]");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.WriteLine($"\t{arg.Source}: {arg.Exception.Message}\n{arg.Exception.StackTrace}");
                }

                return Task.CompletedTask;
            }

        }
    }
}
