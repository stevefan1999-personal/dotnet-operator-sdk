namespace KubeOps.Integration.Test.Operator.Controller
{
    public class ControllerCallCheck
    {
        public int CreateCalled { get; set; }

        public int UpdateCalled { get; set; }

        public int NotModifiedCalled { get; set; }

        public int StatusModifiedCalled { get; set; }

        public int DeletedCalled { get; set; }

        public void Reset()
        {
            CreateCalled = 0;
            UpdateCalled = 0;
            NotModifiedCalled = 0;
            StatusModifiedCalled = 0;
            DeletedCalled = 0;
        }
    }
}
