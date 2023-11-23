using UnityEngine;

namespace JobIt.Runtime.Abstract
{
    public abstract class InvokedUpdateJob<TInvoker,TData> : UpdateJob<TData> where TData : struct  where TInvoker: MonoBehaviour, IJobInvoker
    {
        protected TInvoker Invoker;
        public sealed override bool IsRunning => Invoker.IsRunning;
        protected override int JobPriority => 0;

        public override void Awake()
        {
            base.Awake();
            if (Invoker == null)
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
            Invoker.WithdrawJob(this);
        }
    }
}