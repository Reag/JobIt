using System.Collections.Generic;
using JobIt.Runtime.Abstract;
using UnityEngine;

namespace JobIt.Runtime.Impl.JobScheduler
{
    /// <summary>
    /// The master scheduler and manager for all UpdateJobs.
    /// It acts as a convenient entry point for most actions related to the actual usage of UpdateJobs
    /// </summary>
    public sealed class UpdateJobScheduler
    {
        #region Singleton Setup
        //This region is for a simple singleton design pattern
        private UpdateJobScheduler() { }
        private static UpdateJobScheduler Instance => Nested.Lazy;

        private static class Nested { 
            static Nested() { }
            internal static UpdateJobScheduler Lazy = new();
            //Force Reset for unity load
            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
            static void PlaymodeInit()
            {
                Lazy = new UpdateJobScheduler();
            }
        }
        #endregion

        private readonly Dictionary<System.Type, IUpdateJob> _jobObjectLookup = new();

        /// <summary>
        /// Registers a Component to an UpdateJob, along with any data that might be needed.
        /// A component can only be registered to a particular job once.
        /// Operation takes effect the next time the job is run.
        /// </summary>
        /// <typeparam name="T">The type of UpdateJob 'o' will be registered to</typeparam>
        /// <typeparam name="TS">The type of data that UpdateJob type 'T' expects</typeparam>
        /// <param name="o">The actual component to be registered to a job</param>
        /// <param name="args">The actual data to pass to the job to be associated with 'o'</param>
        public static void Register<T,TS>(Component o, TS args) where T : UpdateJob<TS> where TS : struct
        {
            if (o == null) 
                return;
            //Build the job class if it doesn't exist
            if (!Instance._jobObjectLookup.TryGetValue(typeof(T), out var job)) 
            {
                var jobObject = new GameObject($"{typeof(T)}")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                Object.DontDestroyOnLoad(jobObject);
                job = jobObject.AddComponent<T>();
                Instance._jobObjectLookup[typeof(T)] = job;
            }
            //Pass the data to the actual job class
            (job as T)?.RegisterItem(o, args);
        }

        /// <summary>
        /// Withdraws a Component to an UpdateJob.
        /// Operation takes effect the next time the job is run.
        /// </summary>
        /// <typeparam name="T">The type of UpdateJob 'o' will be withdrawn from</typeparam>
        /// <typeparam name="TS">The type of data that UpdateJob type 'T' expects</typeparam>
        /// <param name="o">The actual component to be withdrawn from a job</param>
        public static void Withdraw<T,TS>(Component o) where T : UpdateJob<TS> where TS : struct
        {
            if (!Instance._jobObjectLookup.TryGetValue(typeof(T), out var job) || job == null) //unity null check
            {
                Debug.LogWarning($"Attempted to Withdraw from a non existing job of type {typeof(T)}!", o);
                return;
            }
            (job as T)?.WithdrawItem(o);
        }

        /// <summary>
        /// Updates the data associated with a particular Component 'o'
        /// Operation takes effect the next time the job is run.
        /// </summary>
        /// <typeparam name="T">The type of UpdateJob that will be updated</typeparam>
        /// <typeparam name="TS">The type of data that UpdateJob type 'T' expects</typeparam>
        /// <param name="o">The actual component the job will look for</param>
        /// <param name="args">The actual data that will be updated</param>
        public static void UpdateJobData<T,TS>(Component o, TS args) where T : UpdateJob<TS> where TS : struct
        {
            if (!Application.isPlaying || o == null)
                return;
            if (!Instance._jobObjectLookup.TryGetValue(typeof(T), out var job) || job == null) //unity null check
            {
                Debug.LogWarning($"Attempted to UpdateJobData for a non existing job of type {typeof(T)}!", o);
                return;
            }
            (job as T)?.UpdateItem(o, args);
        }

        /// <summary>
        /// Gets a direct reference to the GameObject the UpdateJob is attached to.
        /// </summary>
        /// <typeparam name="T">The type of UpdateJob that the GameObject is attached to</typeparam>
        /// <typeparam name="TS">The type of data that UpdateJob type 'T' expects</typeparam>
        /// <returns>A GameObject containing the MonoBehaviour of type T</returns>
        public static T GetJobObject<T,TS>() where T : UpdateJob<TS> where TS :struct
        {
            if (Instance._jobObjectLookup.TryGetValue(typeof(T), out var job))
                return job as T;
            return null;
        }

        /// <summary>
        /// Directly reads the job data associated with a Component
        /// Will fail if the job is currently running!
        /// </summary>
        /// <typeparam name="T">The type of UpdateJob to read from</typeparam>
        /// <typeparam name="TS">The type of data that UpdateJob type 'T' expects</typeparam>
        /// <param name="o">The Component to find data about</param>
        /// <param name="data">The actual data in the UpdateJob referenced</param>
        /// <returns>True if the job exists and the data could be read, false otherwise (Job doesn't exist, Job is Running, or Component is not in Job)</returns>
        public static bool TryReadJobData<T, TS>(Component o, out TS data) where T : UpdateJob<TS> where TS : struct
        {
            return GetJobObject<T, TS>().TryReadItem(o, out data);
        }

        /// <summary>
        /// Disposes of and cleans all jobs currently attached the Scheduler.
        /// Provided for safety in the editor.
        /// </summary>
        public static void CleanJobs()
        {
            var jobs = Instance._jobObjectLookup.Values;
            foreach(var j in jobs)
            {
                if (j == null) continue;
                var g = j.GetGameObject();
                j.Dispose();
                if (g != null)
                {
                    
                    Object.Destroy(g);
                }
            }
            Instance._jobObjectLookup.Clear();
        }
    }
}