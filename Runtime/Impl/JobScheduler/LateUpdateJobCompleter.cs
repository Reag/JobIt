using JobIt.Runtime.Abstract;
using UnityEngine;

namespace JobIt.Runtime.Impl.JobScheduler
{
    /// <summary>
    /// An example JobScheduleCompleter. This will complete its jobs at the end of LateUpdate
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public class LateUpdateJobCompleter : JobScheduleCompleter
    {
        public void LateUpdate()
        {
            CompleteJob();
        }
    }
}