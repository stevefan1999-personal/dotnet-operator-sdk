using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace KubeOps.Integration.Test.Operator
{
    public class OperatorFactory<TStartup> : WebApplicationFactory<TStartup>
        where TStartup : class
    {
        protected override IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(
                    webBuilder => webBuilder
                        .UseStartup<TStartup>());

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSolutionRelativeContentRoot("tests/KubeOps.Integration.Test");
        }
    }
}
