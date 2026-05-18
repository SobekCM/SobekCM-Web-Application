using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SobekCM_Builder_Service;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options => options.ServiceName = "SobekCM Builder Service")
    .ConfigureServices(services => services.AddHostedService<BuilderWorker>())
    .Build();

await host.RunAsync();
