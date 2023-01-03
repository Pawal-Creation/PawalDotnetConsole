using PawalApi;
using System.Text;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PawalDotnetConsole;
internal class Program
{
    private static string SelectPath(string keyword)
    {
        for (UInt16 i = 0;i != UInt16.MaxValue;++i)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(Path.Combine(".",keyword));
            if(i != 0)
            {
                builder.Append("-");
                builder.Append(i);
            }
            builder.Append(".png");
            string path = builder.ToString();
            if(!File.Exists(path))
            {
                return path;
            }
        }
        return string.Empty;
    }


    private static async Task Main(string[] args)
    {
        IServiceCollection services = new ServiceCollection();
        services.AddAnosuApi();
        services.AddLogging(builder => {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        IServiceProvider provider = services.BuildServiceProvider();
        ILogger<Program> logger = provider.GetRequiredService<ILogger<Program>>();
        IPawalApi api = provider.GetRequiredService<IPawalApi>();
        logger.LogInformation(@"
 ____                     _    ____                      _      
|  _ \ __ ___      ____ _| |  / ___|___  _ __  ___  ___ | | ___ 
| |_) / _` \ \ /\ / / _` | | | |   / _ \| '_ \/ __|/ _ \| |/ _ \
|  __/ (_| |\ V  V / (_| | | | |__| (_) | | | \__ \ (_) | |  __/
|_|   \__,_| \_/\_/ \__,_|_|  \____\___/|_| |_|___/\___/|_|\___|                                                                
        ");
        logger.LogInformation("Welcome to use PawalConsole");
        logger.LogInformation("Input a line of string to search image");
        CancellationTokenSource cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender,e)=>
        {
            cts.Cancel();
            logger.LogWarning("Exit console");
            e.Cancel = true;
        };
        logger.LogInformation("Use Ctrl+C to exit");
        while (!cts.Token.IsCancellationRequested)
        {
            string keyword = await Console.In.ReadLineAsync() ?? string.Empty;
            if(keyword == string.Empty)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
            if(cts.Token.IsCancellationRequested)
            {
                logger.LogInformation("Bye");
                return;
            }
            byte[]? image = null;
            try
            {
                image = await api.LookupImageAsync(keyword);
            }
            catch (ApplicationException ex)
            {
                logger.LogError(ex.Message);
                continue;
            }
            catch(HttpRequestException ex)
            {
                logger.LogError(ex.Message);
                continue;
            }
            if(image is null || image.Length == 0)
            {
                logger.LogError($"Cannot find image about {keyword}");
                continue;
            }
            if(keyword == string.Empty)
            {
                keyword = "random";
            }
            string path = SelectPath(keyword);
            if(path == string.Empty)
            {
                logger.LogError($"Cannot keep up,too many files of keyword {keyword}");
                continue;
            }
            using(FileStream fs = new FileStream(path,FileMode.OpenOrCreate,FileAccess.Write,FileShare.ReadWrite,4096,true))
            {
                if(!fs.IsAsync)
                {
                    logger.LogWarning("File I/O executed on synchronized model");
                }
                await fs.WriteAsync(image);
                logger.LogInformation($"Image write to ${path}");
            }
        }
    }
}