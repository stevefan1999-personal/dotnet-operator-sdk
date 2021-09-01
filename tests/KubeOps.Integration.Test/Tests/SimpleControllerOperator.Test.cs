using System.Threading.Tasks;
using FluentAssertions;
using k8s.Models;
using KubeOps.Integration.Test.Operator;
using KubeOps.Integration.Test.Operator.Controller;
using KubeOps.Integration.Test.Operator.Entities;
using KubeOps.Integration.Test.Operator.StartupConfigurations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KubeOps.Integration.Test.Tests
{
    public class SimpleControllerOperatorTest : IClassFixture<OperatorFactory<ControllerOperatorStartup>>
    {
        private readonly OperatorFactory<ControllerOperatorStartup> _factory;

        public SimpleControllerOperatorTest(OperatorFactory<ControllerOperatorStartup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Should_Call_Create_For_Entity()
        {
            var client = _factory.CreateK8sClient();
            var check = _factory.Services.GetRequiredService<ControllerCallCheck>();
            check.Reset();

            check.CreateCalled.Should().Be(0);
            var result = await client.Create(NonRequeueEntity.Create("create-test"));
            await Task.Delay(50);
            check.CreateCalled.Should().Be(1);
            await client.Delete(result);
        }

        [Fact]
        public async Task Should_Call_Update_For_Entity()
        {
            var client = _factory.CreateK8sClient();
            var check = _factory.Services.GetRequiredService<ControllerCallCheck>();
            check.Reset();

            check.UpdateCalled.Should().Be(0);
            var result = await client.Create(NonRequeueEntity.Create("create-test"));
            result.SetAnnotation("test", "value");
            await client.Update(result);
            await Task.Delay(50);
            check.UpdateCalled.Should().Be(1);
            await client.Delete(result);
        }

        [Fact]
        public async Task Should_Call_StatusUpdate_For_Entity()
        {
            var client = _factory.CreateK8sClient();
            var check = _factory.Services.GetRequiredService<ControllerCallCheck>();
            check.Reset();

            check.StatusModifiedCalled.Should().Be(0);
            var result = await client.Create(NonRequeueEntity.Create("create-test"));
            result.Status.SomeStatusValue = "test";
            await client.UpdateStatus(result);
            await Task.Delay(50);
            check.StatusModifiedCalled.Should().Be(1);
            await client.Delete(result);
        }

        [Fact]
        public async Task Should_Call_Delete_For_Entity()
        {
            var client = _factory.CreateK8sClient();
            var check = _factory.Services.GetRequiredService<ControllerCallCheck>();
            check.Reset();

            check.DeletedCalled.Should().Be(0);
            var result = await client.Create(NonRequeueEntity.Create("create-test"));
            await client.Delete(result);
            await Task.Delay(50);
            check.DeletedCalled.Should().Be(1);
        }
    }
}
