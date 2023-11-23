using JobIt.Runtime.Impl.JobScheduler;

namespace JobIt.Runtime.Abstract
{
    public abstract class LateUpdateToLateUpdateJob<T> : InvokedUpdateJob<LateUpdateJobInvoker, T> where T : struct
    {
    }
}