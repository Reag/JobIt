using UnityEngine;

namespace JobIt.Runtime.Abstract
{
    /// <summary>
    /// An IUpdateJob that automatically subscribes and withdraws itself from a particular IJobInvoker
    /// </summary>
    /// <typeparam name="TInvoker">The IJobInvoker type this class with Register and Withdraw from</typeparam>
    /// <typeparam name="TData">The type of data this IUpdateJob will be expecting</typeparam>
    public abstract class InvokedUpdateJob<TInvoker,TData> : UpdateJob<TData> where TData : struct  where TInvoker: MonoBehaviour, IJobInvoker
    {
        protected TInvoker Invoker;
        /// <summary>
        /// This job is considered running with the Invoker is running
        /// </summary>
        public sealed override bool IsRunning => Invoker.IsRunning;

        public sealed override void Awake()
        {
            base.Awake();
            if (Invoker == null) //Subscribe to the singleton of the Invoker
            {
                Invoker = JobScheduleInvoker<TInvoker>.Instance;
            }
            Invoker.RegisterJob(this, JobPriority);
        }

        protected sealed override void CompleteJob()
        {
            //Do nothing, as this particular job is completed by the TInvoker
        }

        protected override void DisposeLogic()
        {
            //When disposing of this job, be sure to withdraw from the Invoker!
            Invoker.WithdrawJob(this);
        }
    }
}