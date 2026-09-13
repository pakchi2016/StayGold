using System;
using System.Diagnostics;
using System.Text.Json;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Reflection;

namespace DreamJourney
{
    class Program
    {
        private const string PipeName = "StayGoldPipe";
        private const string ServicApp = @"C:\work\Module\Orfevre.exe";

        static async Task Main(string[] args)
        {
            if (args.Length == 0) args[0] = "Start";
            NamedPipeClientStream pipeClientStream;
            var command = args;
            try
            {
                pipeClientStream = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                await pipeClientStream.ConnectAsync(100);
            }
            catch (TimeoutException)
            {
                if(command[0] == "Close") return;
                if (File.Exists(ServicApp)) Process.Start(ServicApp);
                    else return;
                pipeClientStream = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                for(int i = 0; i < 10; i++)
                {
                    try
                    {
                        await pipeClientStream.ConnectAsync(100);
                        break;
                    }
                    catch (TimeoutException)
                    {
                        await Task.Delay(100);
                    }
                }
            }

            if (!pipeClientStream.IsConnected) return;
            using (var writer = new StreamWriter(pipeClientStream) { AutoFlush = true })
            {
                // コマンドをJson形式で送信する
                var commandJson = JsonSerializer.Serialize(command);
                await writer.WriteLineAsync(commandJson);
            }
        }
    }
}