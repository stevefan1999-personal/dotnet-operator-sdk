using System.Threading.Tasks;
using KubeOps.Integration.Test.Operator;
using KubeOps.Integration.Test.Operator.StartupConfigurations;
using Xunit;

namespace KubeOps.Integration.Test.Tests
{
    public class EmptyOperatorTest : IClassFixture<OperatorFactory<EmptyOperatorStartup>>
    {
        private readonly OperatorFactory<EmptyOperatorStartup> _factory;

        public EmptyOperatorTest(OperatorFactory<EmptyOperatorStartup> factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData("/ready")]
        [InlineData("/health")]
        [InlineData("/metrics")]
        public async Task Should_Provide_Default_Endpoints(string url)
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
        }
    }
}
