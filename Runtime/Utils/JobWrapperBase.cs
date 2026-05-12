using System;
using JobIt.Runtime.Abstract;
using JobIt.Runtime.Impl.JobScheduler;
using UnityEngine;

namespace JobIt.Runtime.Utils
{
    public interface IJobWrapperBase : IDisposable
    {
        void RegisterToJob();
        void WithdrawFromJob();
    }

    public abstract class JobWrapperBase<TJob, TJobData> : IJobWrapperBase 
        where TJob : UpdateJob<TJobData> 
        where TJobData : struct
    {
        protected readonly Component Owner;
        protected bool isRegistered { get; private set; }
        protected bool disposed { get; private set; }
        protected TJobData currentData { get; private set; }

        protected JobWrapperBase(Component owner, TJobData data)
        {
            Owner = owner;
            currentData = data;
            RegisterToJob(data);
        }

        public void RegisterToJob()
        {
            RegisterToJob(currentData);
        }

        public void RegisterToJob(TJobData data)
        {
            if (disposed || isRegistered) return;
            isRegistered = true;
            RegisterToJobInternal(data);
        }

        public void UpdateJobData(TJobData data)
        {
            if (disposed) return;
            currentData = data;
            UpdateJobInternal(data);
        }

        public void WithdrawFromJob()
        {
            if (!isRegistered) return;
            WithdrawFromJobInternal();
            isRegistered = false;
        }

        protected virtual void RegisterToJobInternal(TJobData data)
        {
            UpdateJobScheduler.Register<TJob, TJobData>(Owner, data);
        }

        protected virtual void UpdateJobInternal(TJobData data)
        {
            UpdateJobScheduler.UpdateJobData<TJob, TJobData>(Owner, data);
        }

        protected virtual void WithdrawFromJobInternal()
        {
            UpdateJobScheduler.Withdraw<TJob, TJobData>(Owner);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            WithdrawFromJob();
            disposed = true;
        }
    }
}