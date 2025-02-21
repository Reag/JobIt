using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JobIt.Runtime.Abstract
{
    /// <summary>
    /// Simple interface to represent some kind of JobInvoker
    /// </summary>
    public interface IJobInvoker
    {
        public bool IsRunning { get; }
        public void RegisterJob(IUpdateJob job, int executionOrder = 0);
        public void WithdrawJob(IUpdateJob job);
    }


    /// <summary>
    /// This Abstract Class represents some strategy for starting a managed job, typically via update.
    /// Subclasses should self reference to allow singleton access to the invoker
    /// </summary>
    /// <typeparam name="TInvoker">A self reference to the type of the subclass inheriting from this class</typeparam>
    public abstract class JobScheduleInvoker<TInvoker> : MonoBehaviour where TInvoker : MonoBehaviour, IJobInvoker
    {
        private static TInvoker _instance;

        /// <summary>
        /// Simple MonoBehaviour Singleton design pattern
        /// </summary>
        public static TInvoker Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = FindAnyObjectByType<TInvoker>(); //look for scene objects
                if (_instance != null) return _instance;

                var obj = new GameObject
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                _instance = obj.AddComponent<TInvoker>();
                return _instance;
            }
        }

        /// <summary>
        /// Is the Invoker currently running a job?
        /// </summary>
        public bool IsRunning { get; private set; }
        /// <summary>
        /// The last combined JobHandle that this Invoker began. Provided for safety in the editor.
        /// </summary>
        public JobHandle CurrentDependency { get; set; }
        /// <summary>
        /// The Completer that will complete the JobHandle referenced in CurrentDependency
        /// </summary>
        private JobScheduleCompleter _completer;

        public struct OrderedJob : IComparable<OrderedJob>
        {
            public int ExecutionOrder;
            public IUpdateJob Job;

            public readonly int CompareTo(OrderedJob other)
            {
                return ExecutionOrder.CompareTo(other.ExecutionOrder);
            }
        }

        /// <summary>
        /// Simple SortedList to organize our registered jobs by ExecutionOrder
        /// </summary>
        protected List<OrderedJob> JobList;

        /// <summary>
        /// Reusable memory block to allow us to combine job handles with the same ExecutionOrder
        /// </summary>
        protected NativeList<JobHandle> CurrentHandles;

        [ExcludeFromCoverage]
        protected virtual void Awake()
        {
            JobList = new List<OrderedJob>();
        }

        [ExcludeFromCoverage] // Ensure safe exit when running in the editor
        protected virtual void OnDestroy()
        {
            CurrentDependency.Complete();
            Destroy(_completer);
        }

        /// <summary>
        /// Adds a particular Completer to this Invoker. Said Completer will manege ensuring the jobs complete according to its internal strategy.
        /// </summary>
        /// <typeparam name="T">The type of Completer to add</typeparam>
        protected virtual void AddCompleter<T>() where T : JobScheduleCompleter
        {
            _completer = gameObject.AddComponent<T>();
            _completer.OnComplete += OnJobComplete;
        }

        /// <summary>
        /// Called when the added Completer actually completes the job, setting IsRunning to false.
        /// This is also when EndJob is called on all registered UpdateJobs
        /// </summary>
        protected virtual void OnJobComplete()
        {
            IsRunning = false;
            for (var i = 0; i < JobList.Count; i++)
            {
                JobList[i].Job.EndJob();
            }
        }

        /// <summary>
        /// Register an IUpdateJob to this Invoker
        /// </summary>
        /// <param name="job">The IUpdateJob to be registered</param>
        /// <param name="executionOrder">What execution order the IUpdateJob should have</param>
        public virtual void RegisterJob(IUpdateJob job, int executionOrder = 0)
        {
            JobList.Add(new OrderedJob { ExecutionOrder = executionOrder, Job = job });
            JobList.Sort();
        }

        /// <summary>
        /// Withdraw an IUpdateJob from this Invoker
        /// </summary>
        /// <param name="job">The IUpdateJob to withdraw</param>
        public virtual void WithdrawJob(IUpdateJob job)
        {
            JobList.RemoveAll(x => x.Job == job);
            JobList.Sort();
        }

        /// <summary>
        /// Internal logic for actually running the registered jobs. It processes the jobs by execution order, creating dependencies for each execution step.
        /// If two jobs share an execution order, it will attempt to build a combined dependency.
        /// </summary>
        protected virtual void RunJobs()
        {
            if (JobList.Count == 0 || IsRunning) return;
            IsRunning = true;
            CurrentDependency = default;
            CurrentHandles = new NativeList<JobHandle>(10, Allocator.Temp);
            var currentPriority = JobList[0].ExecutionOrder;

            // Execute the job setup step. 
            for (var i = 0; i < JobList.Count; i++)
            {
                JobList[i].Job.PreStartJob();
            }

            for (var i = 0; i < JobList.Count; i++)
            {
                var p = JobList[i].ExecutionOrder;
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