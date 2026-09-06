using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace DreamJourney
{
    class Program
    {
        private const string PipeName = "StayGoldPipe";
        private const string ServicApp = @"C:\work\Module\Orfevre.exe";

        static async Task Main(string[] args)
        {
            // 引数がなければ何もしないで終了
            if (args.Length == 0) return;
            NamedPipeClientStream pipeClientStream;
            var command = args[0];
            try
            {
                pipeClientStream = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                await pipeClientStream.ConnectAsync(100);
            }
            catch (TimeoutException)
            {
                if(File.Exists(ServicApp)) Process.Start(ServicApp);
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
                // コマンドを送信
                await writer.WriteLineAsync(command);
            }
        }
    }
}