using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;

namespace JobIt.Runtime.Abstract
{
    public interface IJobInvoker
    {
        public bool IsRunning { get; }
        public void RegisterJob(IUpdateJob job, int priority = 0);
        public void WithdrawJob(IUpdateJob job);
    }

    public abstract class JobScheduleInvoker<TInvoker> : MonoBehaviour where TInvoker: MonoBehaviour, IJobInvoker
    {
        public static TInvoker Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var obj = new GameObject {
                    hideFlags = HideFlags.HideAndDontSave
                };
                DontDestroyOnLoad(obj);
                _instance = obj.AddComponent<TInvoker>();
                return _instance;
            }
        }
        private static TInvoker _instance;

        public bool IsRunning { get; private set; }
        public JobHandle CurrentDependency { get; set; }
        private JobScheduleCompleter _completer;
        protected struct OrderedJob : IComparable<OrderedJob>
        {
            public int Priority;
            public IUpdateJob Job;

            public readonly int CompareTo(OrderedJob other)
            {
                return Priority.CompareTo(other.Priority);
            }
        }

        protected List<OrderedJob> JobList;
        protected NativeList<JobHandle> CurrentHandles;

        protected virtual void Awake()
        {
            JobList = new List<OrderedJob>();
        }

        protected virtual void OnDestroy()
        {
            CurrentDependency.Complete();
            Destroy(_completer);
        }

        protected virtual void AddCompleter<T>() where T : JobScheduleCompleter
        {
            _completer = gameObject.AddComponent<T>();
            _completer.OnComplete += OnJobComplete;
        }

        protected virtual void OnJobComplete()
        {
            IsRunning = false;
            for (var i = 0; i < JobList.Count; i++)
            {
                JobList[i].Job.EndJob();
            }
        }

        public virtual void RegisterJob(IUpdateJob job, int priority = 0)
        {
            JobList.Add(new OrderedJob { Priority = priority, Job = job });
            JobList.Sort();
        }

        public virtual void WithdrawJob(IUpdateJob job)
        {
            JobList.RemoveAll(x => x.Job == job);
            JobList.Sort();
        }

        protected virtual void RunJobs()
        {
            if (JobList.Count == 0 || IsRunning) return;
            IsRunning = true;
            CurrentDependency = default;
            CurrentHandles = new NativeList<JobHandle>(10, Allocator.Temp);
            var currentPriority = JobList[0].Priority;

            for (var i = 0; i < JobList.Count; i++)
            {
                var p = JobList[i].Priority;
                var j = JobList[i].Job;
                if (p != currentPriority) //Priority has updated
                {
                    currentPriority = p;
                    CurrentDependency = JobHandle.CombineDependencies(CurrentHandles.AsArray());
                    CurrentHandles.Clear();
                }
                JobHandle h = default;
                try
                {
                    Profiler.BeginSample("JobScheduleInvoker: Start Job " + j.GetType());
                    h = j.StartJob(CurrentDependency);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to run job {j.GetGameObject().name}! Stack Trace Below:");
                    Debug.LogWarning(e);
                }
                finally
                {
                    Profiler.EndSample();
                }
                CurrentHandles.Add(h);
            }
            CurrentDependency = JobHandle.CombineDependencies(CurrentHandles.AsArray());
            _completer.Job = CurrentDependency;
            CurrentHandles.Dispose();
        }
    }
}