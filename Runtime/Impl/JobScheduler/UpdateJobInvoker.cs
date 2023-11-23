using JobIt.Runtime.Abstract;
using UnityEngine;

namespace JobIt.Runtime.Impl.JobScheduler
{
    [DefaultExecutionOrder(100)]
    public class UpdateJobInvoker : JobScheduleInvoker<UpdateJobInvoker>, IJobInvoker
    {
        protected override void Awake()
        {
            base.Awake();
            AddCompleter<UpdateJobCompleter>();
        }

        public void Update()
        {
            RunJobs();
        }
    }

    [DefaultExecutionOrder(-1000)]
    public class UpdateJobCompleter : JobScheduleCompleter
    {
        public void LateUpdate()
        {
            CompleteJob();
        }
    }
}