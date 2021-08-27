using System;
using System.Collections.Generic;
using System.Linq;
using DotnetKubernetesClient;
using k8s.Models;
using KubeOps.Integration.Test.Operator.Entities;
using KubeOps.Operator.Builder;
using KubeOps.Operator.Entities;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestFramework("KubeOps.Integration.Test.CrdInstaller", "KubeOps.Integration.Test")]

namespace KubeOps.Integration.Test
{
    public class CrdInstaller : XunitTestFramework, IDisposable
    {
        private readonly IReadOnlyList<V1CustomResourceDefinition> _crds;

        public CrdInstaller(IMessageSink sink)
            : base(sink)
        {
            var registrar = new ComponentRegistrar();

            registrar.RegisterEntity<TestEntityWithoutSpec>();

            var builder = new CrdBuilder(registrar);
            _crds = builder.BuildCrds().ToList();

            var client = new KubernetesClient();
            foreach (var crd in _crds)
            {
                var _ = client.Save(crd).Result;
            }
        }

        public new void Dispose()
        {
            var client = new KubernetesClient();
            foreach (var crd in _crds)
            {
                client.Delete(crd).Wait();
            }
            base.Dispose();
        }
    }
}
