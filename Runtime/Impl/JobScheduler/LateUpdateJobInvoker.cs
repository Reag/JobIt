using JobIt.Runtime.Abstract;
using UnityEngine;

namespace JobIt.Runtime.Impl.JobScheduler
{
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