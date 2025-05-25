using Microsoft.Extensions.DependencyInjection;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.ServiceProvider;
using Thunders.TechTest.ApiService.Messages;
using Thunders.TechTest.ApiService.Handlers;

namespace Thunders.TechTest.ApiService.Extensions
{
    public static class RebusExtensions
    {
        public static void AddRebus(this IServiceCollection services, IConfiguration configuration)
        {
             services.AddTransient<UtilizacaoMessageHandler>();

            services.AddRebus(configure => configure
                .Transport(t => t.UseRabbitMq(
                    connectionString: configuration.GetConnectionString("RabbitMq") ?? "amqp://localhost",
                    inputQueueName: "thunders-utilizacoes"
                ))
                .Routing(r => r.TypeBased()
                    .Map<UtilizacaoMessage>("thunders-utilizacoes") // ESTA É A LINHA IMPORTANTE!
                )
                .Options(o => {
                    o.SetNumberOfWorkers(5); // 5 workers em paralelo
                    o.SetMaxParallelism(10); // Máximo 10 mensagens simultâneas
                }));

            services.AutoRegisterHandlersFromAssemblyOf<UtilizacaoMessageHandler>();
        }
    }
}
