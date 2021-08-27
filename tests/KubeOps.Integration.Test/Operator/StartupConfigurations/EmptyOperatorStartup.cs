using KubeOps.Operator;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Integration.Test.Operator.StartupConfigurations
{
    public class EmptyOperatorStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddKubernetesOperator(
                    s =>
                    {
                        s.EnableLeaderElection = false;
                        s.EnableAssemblyScanning = false;
                    });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseKubernetesOperator();
        }
    }
}
