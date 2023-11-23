using UnityEngine;

namespace JobIt.Runtime.Abstract
{
    public abstract class InvokedUpdateJob<TInvoker,TData> : UpdateJob<TData> where TData : struct  where TInvoker: MonoBehaviour, IJobInvoker
    {
        protected TInvoker Invoker;
        public override sealed bool IsRunning { get => Invoker.IsRunning; }
        protected override int JobPriority { get => 0; }

        public override void Awake()
        {
            base.Awake();
            if (Invoker == null)
            {
                Invoker = JobScheduleInvoker<TInvoker>.Instance;
            }
            Invoker.RegisterJob(this, JobPriority);
        }

        protected override sealed void CompleteJob()
        {
            //Do nothing, as this particular job is completed by the LateUpdateScheduleCompleter
        }

        public override void Dispose()
        {
            base.Dispose();
            Invoker.WithdrawJob(this);
        }
    }
}