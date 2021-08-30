using System.Threading.Tasks;
using KubeOps.Integration.Test.Operator.Entities;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;

namespace KubeOps.Integration.Test.Operator.Controller
{
    public class NonRequeueingController : IResourceController<TestEntityWithoutSpec>
    {
        public async Task<ResourceControllerResult?> CreatedAsync(TestEntityWithoutSpec entity)
        {
            throw new System.NotImplementedException();
        }

        public async Task<ResourceControllerResult?> UpdatedAsync(TestEntityWithoutSpec entity)
        {
            throw new System.NotImplementedException();
        }

        public async Task<ResourceControllerResult?> NotModifiedAsync(TestEntityWithoutSpec entity)
        {
            throw new System.NotImplementedException();
        }

        public async Task StatusModifiedAsync(TestEntityWithoutSpec entity)
        {
            throw new System.NotImplementedException();
        }

        public async Task DeletedAsync(TestEntityWithoutSpec entity)
        {
            throw new System.NotImplementedException();
        }
    }
}
