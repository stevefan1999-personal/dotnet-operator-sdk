using k8s.Models;
using KubeOps.Operator.Entities;

namespace KubeOps.Integration.Test.Operator.Entities
{
    [KubernetesEntity(Group = "kubeops.integration.testing", ApiVersion = "v1")]
    public class TestEntityWithoutSpec : CustomKubernetesEntity
    {
        public static TestEntityWithoutSpec Create(string name = "test-instance", string @namespace = "default") =>
            new()
            {
                Kind = "TestEntityWithoutSpec",
                ApiVersion = "kubeops.integration.testing/v1",
                Metadata = new V1ObjectMeta
                {
                    Name = name,
                    NamespaceProperty = @namespace,
                },
            };
    }
}
