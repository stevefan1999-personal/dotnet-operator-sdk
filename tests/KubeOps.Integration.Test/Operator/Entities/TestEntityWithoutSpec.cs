using k8s.Models;
using KubeOps.Operator.Entities;

namespace KubeOps.Integration.Test.Operator.Entities
{
    [KubernetesEntity(Group = "kubeops.integration.testing", ApiVersion = "v1")]
    public class TestEntityWithoutSpec : CustomKubernetesEntity
    {
    }
}
