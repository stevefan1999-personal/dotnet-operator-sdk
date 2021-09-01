using System.Threading.Tasks;
using KubeOps.Integration.Test.Operator.Entities;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;

namespace KubeOps.Integration.Test.Operator.Controller
{
    public class NonRequeueingController : IResourceController<NonRequeueEntity>
    {
        private readonly ControllerCallCheck _check;

        public NonRequeueingController(ControllerCallCheck check)
        {
            _check = check;
        }

        public Task<ResourceControllerResult?> CreatedAsync(NonRequeueEntity entity)
        {
            _check.CreateCalled++;
            return Task.FromResult<ResourceControllerResult?>(null);
        }

        public Task<ResourceControllerResult?> UpdatedAsync(NonRequeueEntity entity)
        {
            _check.UpdateCalled++;
            return Task.FromResult<ResourceControllerResult?>(null);
        }

        public Task StatusModifiedAsync(NonRequeueEntity entity)
        {
            _check.StatusModifiedCalled++;
            return Task.FromResult<ResourceControllerResult?>(null);
        }

        public Task DeletedAsync(NonRequeueEntity entity)
        {
            _check.DeletedCalled++;
            return Task.FromResult<ResourceControllerResult?>(null);
        }
    }
}
