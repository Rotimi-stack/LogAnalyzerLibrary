using LogAnalyzerLibrary;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
         .ConfigureAppConfiguration(configuration =>
         {
             configuration.AddJsonFile(
                 $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json",
                 optional: true, reloadOnChange: true);
         })

            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseKestrel();
                webBuilder.UseIISIntegration();
                webBuilder.UseStartup<Startup>();
            });
}