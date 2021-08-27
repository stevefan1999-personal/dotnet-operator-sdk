using KubeOps.Integration.Test.Operator.Entities;
using KubeOps.Operator.Controller;

namespace KubeOps.Integration.Test.Operator.Controller
{
    public class NonRequeueingController : IResourceController<TestEntityWithoutSpec>
    {
    }
}
