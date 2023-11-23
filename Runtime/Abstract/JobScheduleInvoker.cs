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
                if (_instance == null)
                {
                    var obj = new GameObject {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    DontDestroyOnLoad(obj);
                    _instance = obj.AddComponent<TInvoker>();
                }
                return _instance;
            }
        }
        private static TInvoker _instance;

        public bool IsRunning { get; private set; }
        public JobHandle CurrentDependency { get; set; }
        private JobScheduleCompleter _completer;
        protected struct OrderedJob : IComparable<OrderedJob>
        {
            public int priority;
            public IUpdateJob job;

            public readonly int CompareTo(OrderedJob other)
            {
                return priority.CompareTo(other.priority);
            }
        }

        protected List<OrderedJob> _jobList;
        protected NativeList<JobHandle> _currentHandles;

        protected virtual void Awake()
        {
            _jobList = new List<OrderedJob>();
            
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
            for (int i = 0; i < _jobList.Count; i++)
            {
                _jobList[i].job.EndJob();
            }
        }

        public virtual void RegisterJob(IUpdateJob job, int priority = 0)
        {
            _jobList.Add(new OrderedJob { priority = priority, job = job });
            _jobList.Sort();
        }

        public virtual void WithdrawJob(IUpdateJob job)
        {
            _jobList.RemoveAll(x => x.job == job);
            _jobList.Sort();
        }

        protected virtual void RunJobs()
        {
            if (_jobList.Count == 0 || IsRunning) return;
            IsRunning = true;
            CurrentDependency = default;
            _currentHandles = new NativeList<JobHandle>(10, Allocator.Temp);
            int currentPriority = _jobList[0].priority;

            for (int i = 0; i < _jobList.Count; i++)
            {
                int p = _jobList[i].priority;
                var j = _jobList[i].job;
                if (p != currentPriority) //Priority has updated
                {
                    currentPriority = p;
                    CurrentDependency = JobHandle.CombineDependencies(_currentHandles.AsArray());
                    _currentHandles.Clear();
                }
                JobHandle h = default;
                try
                {
                    Profiler.BeginSample("Start Job " + j.GetType());
                    h = j.StartJob(CurrentDependency);
                    Profiler.EndSample();
                } 
                catch(Exception e)
                {
                    Debug.LogWarning($"Failed to run job {j.GetGameObject().name}! Stack Trace Below:");
                    Debug.LogWarning(e);
                }
                _currentHandles.Add(h);
            }
            CurrentDependency = JobHandle.CombineDependencies(_currentHandles.AsArray());
            _completer.Job = CurrentDependency;
            _currentHandles.Dispose();
        }
    }
}