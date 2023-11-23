using JobIt.Runtime.Abstract;
using UnityEngine;

namespace JobIt.Runtime.Impl.JobScheduler
{
    [DefaultExecutionOrder(10000)]
    public class LateUpdateJobCompleter : JobScheduleCompleter
    {
        public void LateUpdate()
        {
            CompleteJob();
        }
    }
}