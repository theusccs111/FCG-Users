using FCG_Users.Consumer;

namespace FCG_USers.Consumer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddConsumerServices(hostContext.Configuration);
                    services.AddHostedService<Worker>();
                });
    }
}