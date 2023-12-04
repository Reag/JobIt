using JobIt.Runtime.Abstract;
using UnityEngine;

namespace JobIt.Runtime.Impl.JobScheduler
{
    /// <summary>
    /// An example JobScheduleInvoker. It will start its jobs later on in LateUpdate, and complete them by the end of LateUpdate
    /// </summary>
    [DefaultExecutionOrder(6)]
    public class LateUpdateJobInvoker : JobScheduleInvoker<LateUpdateJobInvoker>, IJobInvoker
    {
        protected override void Awake()
        {
            base.Awake();
            AddCompleter<LateUpdateJobCompleter>();
        }

        public void LateUpdate()
        {
           RunJobs();
        }
    }
}