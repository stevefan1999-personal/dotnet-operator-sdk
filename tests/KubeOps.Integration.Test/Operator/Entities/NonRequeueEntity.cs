using k8s.Models;
using KubeOps.Operator.Entities;

namespace KubeOps.Integration.Test.Operator.Entities
{
    [KubernetesEntity(Group = "kubeops.integration.testing", ApiVersion = "v1")]
    public class NonRequeueEntity : CustomKubernetesEntity<NonRequeueEntity.EntitySpec,
        NonRequeueEntity.EntityStatus>
    {
        public static NonRequeueEntity Create(
            string name = "test-instance",
            string @namespace = "default") =>
            new()
            {
                Kind = "NonRequeueEntity",
                ApiVersion = "kubeops.integration.testing/v1",
                Metadata = new V1ObjectMeta
                {
                    Name = name,
                    NamespaceProperty = @namespace,
                },
            };

        public class EntitySpec
        {
            public string SomeSpecValue { get; set; } = string.Empty;
        }

        public class EntityStatus
        {
            public string SomeStatusValue { get; set; } = string.Empty;
        }
    }
}
