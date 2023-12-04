using JobIt.Runtime.Abstract;
using UnityEngine;

namespace JobIt.Runtime.Impl.JobScheduler
{
    /// <summary>
    /// An example JobScheduleInvoker. It will start its jobs near the end of Update and complete them early on in LateUpdate
    /// </summary>
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

    /// <summary>
    /// An example JobScheduleCompleter. It will complete its jobs near the start of LateUpdate
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class UpdateJobCompleter : JobScheduleCompleter
    {
        public void LateUpdate()
        {
            CompleteJob();
        }
    }
}