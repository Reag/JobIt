using System.Collections.Generic;
using JobIt.Runtime.Abstract;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
        private UpdateJobScheduler()
        {
        }

        private static UpdateJobScheduler Instance => Nested.Lazy;

        private static class Nested
        {
            static Nested()
            {
            }

            internal static UpdateJobScheduler Lazy = new();

            //Force Reset for unity load
            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
            static void PlaymodeInit()
            {
                Lazy = new UpdateJobScheduler();
#if UNITY_EDITOR
                //We use this to prevent memory leaks when unexpectedly entering or exiting playmode.
                EditorApplication.playModeStateChanged += PlayModeStateChange;
            }

            private static void PlayModeStateChange(PlayModeStateChange obj)
            {
                if (obj is UnityEditor.PlayModeStateChange.EnteredEditMode
                    or UnityEditor.PlayModeStateChange.ExitingEditMode)
                {
                    CleanJobs();
                }
#endif
            }
        }

        #endregion

        private readonly Dictionary<System.Type, IUpdateJob> _jobObjectLookup = new();

        /// <summary>
        /// Registers a Component to an UpdateJob, along with any data that might be needed.
        /// A component can only be registered to a particular job once.
        /// Operation takes effect the next time the job is run.
        /// </summary>
        /// <typeparam name="TJob">The type of UpdateJob 'o' will be registered to</typeparam>
        /// <typeparam name="TJobData">The type of data that UpdateJob type 'T' expects</typeparam>
        /// <param name="o">The actual component to be registered to a job</param>
        /// <param name="args">The actual data to pass to the job to be associated with 'o'</param>
        public static void Register<TJob, TJobData>(Component o, TJobData args) 
            where TJob : UpdateJob<TJobData> 
            where TJobData : struct
        {
            if (o == null)
            {
                return;
            }

            //Build the job class if it doesn't exist
            if (!Instance._jobObjectLookup.TryGetValue(typeof(TJob), out var job))
            {
                var jobObject = new GameObject($"{typeof(TJob)}")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                job = jobObject.AddComponent<TJob>();
                Instance._jobObjectLookup[typeof(TJob)] = job;
            }

            //Pass the data to the actual job class
            (job as TJob)?.RegisterItem(o, args);
        }

        /// <summary>
        /// Withdraws a Component to an UpdateJob.
        /// Operation takes effect the next time the job is run.
        /// </summary>
        /// <typeparam name="TJob">The type of UpdateJob 'o' will be withdrawn from</typeparam>
        /// <typeparam name="TJobData">The type of data that UpdateJob type 'T' expects</typeparam>
        /// <param name="o">The actual component to be withdrawn from a job</param>
        public static void Withdraw<TJob, TJobData>(Component o) 
            where TJob : UpdateJob<TJobData> 
            where TJobData : struct
        {
            if (!Instance._jobObjectLookup.TryGetValue(typeof(TJob), out var job) || job == null) //unity null check
            {
                Debug.LogWarning($"Attempted to Withdraw from a non existing job of type {typeof(TJob)}!", o);
                return;
            }

            (job as TJob)?.WithdrawItem(o);
        }

        /// <summary>
        /// Updates the data associated with a particular Component 'o'
        /// Operation takes effect the next time the job is run.
        /// </summary>
        /// <typeparam name="TJob">The type of UpdateJob that will be updated</typeparam>
        /// <typeparam name="TJobData">The type of data that UpdateJob type 'T' expects</typeparam>
        /// <param name="o">The actual component the job will look for</param>
        /// <param name="args">The actual data that will be updated</param>
        public static void UpdateJobData<TJob, TJobData>(Component o, TJobData args) 
            where TJob : UpdateJob<TJobData> 
            where TJobData : struct
        {
            if (!Application.isPlaying || o == null)
            {
                return;
            }

            if (!Instance._jobObjectLookup.TryGetValue(typeof(TJob), out var job) || job == null) //unity null check
            {
                Debug.LogWarning($"Attempted to UpdateJobData for a non existing job of type {typeof(TJob)}!", o);
                return;
            }

            (job as TJob)?.UpdateItem(o, args);
        }

        /// <summary>
        /// Gets a direct reference to the GameObject the UpdateJob is attached to.
        /// </summary>
        /// <typeparam name="TJob">The type of UpdateJob that the GameObject is attached to</typeparam>
        /// <typeparam name="TJobData">The type of data that UpdateJob type 'T' expects</typeparam>
        /// <returns>A GameObject containing the MonoBehaviour of type T</returns>
        public static TJob GetJobObject<TJob, TJobData>() 
            where TJob : UpdateJob<TJobData> 
            where TJobData : struct
        {
            if (Instance._jobObjectLookup.TryGetValue(typeof(TJob), out var job))
            {
                return job as TJob;
            }

            return null;
        }

        /// <summary>
        /// Directly reads the job data associated with a Component
        /// Will fail if the job is currently running!
        /// </summary>
        /// <typeparam name="TJob">The type of UpdateJob to read from</typeparam>
        /// <typeparam name="TJobData">The type of data that UpdateJob type 'T' expects</typeparam>
        /// <param name="o">The Component to find data about</param>
        /// <param name="data">The actual data in the UpdateJob referenced</param>
        /// <returns>True if the job exists and the data could be read, false otherwise (Job doesn't exist, Job is Running, or Component is not in Job)</returns>
        public static bool TryReadJobData<TJob, TJobData>(Component o, out TJobData data) 
            where TJob : UpdateJob<TJobData> 
            where TJobData : struct
        {
            var jobObject = GetJobObject<TJob, TJobData>();
            if (jobObject != null)
            {
                return jobObject.TryReadItem(o, out data);
            }

            data = default;
            return false;
        }

        /// <summary>
        /// Disposes of and cleans all jobs currently attached the Scheduler.
        /// Provided for safety in the editor.
        /// </summary>
        public static void CleanJobs()
        {
            var jobs = Instance._jobObjectLookup.Values;
            foreach (var j in jobs)
            {
                if (j == null)
                {
                    continue;
                }

                var g = j.GetGameObject();
                j.Dispose();
                if (g != null)
                {
                    Object.DestroyImmediate(g);
                }
            }

            Instance._jobObjectLookup.Clear();
        }
    }
}