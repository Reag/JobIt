using JobIt.Runtime.Impl.JobScheduler;

namespace JobIt.Runtime.Abstract
{
    /// <summary>
    /// A sample abstract InvokedUpdateJob class for a job that runs on Update and ends on LateUpdate
    /// </summary>
    /// <typeparam name="T">a struct containing the job data</typeparam>
    public abstract class UpdateToUpdateJob<T> : InvokedUpdateJob<UpdateJobInvoker, T> where T : struct
    {
    }
}