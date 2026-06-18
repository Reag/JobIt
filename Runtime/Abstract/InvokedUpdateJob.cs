using UnityEngine;

namespace JobIt.Runtime.Abstract
{
    /// <summary>
    /// An IUpdateJob that automatically subscribes and withdraws itself from a particular IJobInvoker
    /// </summary>
    /// <typeparam name="TInvoker">The IJobInvoker type this class with Register and Withdraw from</typeparam>
    /// <typeparam name="TData">The type of data this IUpdateJob will be expecting</typeparam>
    public abstract class InvokedUpdateJob<TInvoker, TData> : UpdateJob<TData>
        where TData : struct where TInvoker : MonoBehaviour, IJobInvoker
    {
        private TInvoker _invoker;

        /// <summary>
        /// This job is considered running with the Invoker is running
        /// </summary>
        public sealed override bool IsRunning => _invoker.IsRunning;

        public sealed override void Awake()
        {
            base.Awake();
            if (_invoker == null) //Subscribe to the singleton of the Invoker
            {
                _invoker = JobScheduleInvoker<TInvoker>.Instance;
            }

            _invoker.RegisterJob(this, JobPriority);
        }

        protected sealed override void CompleteJob()
        {
            //Do nothing, as this particular job is completed by the TInvoker
        }

        protected sealed override void DisposeLogic()
        {
            //When disposing of this job, be sure to withdraw from the Invoker!
            _invoker.WithdrawJob(this);
            DisposeLogicInternal();
        }

        /// <summary>
        /// Put custom dispose logic here, instead of the normal DisposeLogic function
        /// </summary>
        protected virtual void DisposeLogicInternal(){ }
    }
}