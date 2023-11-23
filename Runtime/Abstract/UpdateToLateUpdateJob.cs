using JobIt.Runtime.Impl.JobScheduler;

namespace JobIt.Runtime.Abstract
{
    public abstract class UpdateToUpdateJob<T> : InvokedUpdateJob<UpdateJobInvoker, T> where T : struct
    {
    }
}