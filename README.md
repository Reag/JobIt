# JobIt

A lightweight Unity Job System manager that removes the boilerplate from writing and scheduling high-performance multithreaded jobs.

---

## Highlights

- **Less boilerplate** — stop manually managing `JobHandle`s, `NativeContainer` lifecycles, and completion calls; JobIt handles all of it
- **Automatic lifecycle integration** — jobs schedule on `Update` and complete on `LateUpdate` (or both on `LateUpdate`) with zero setup
- **Burst-Focused** — designed to manage with Burst-compiled jobs
- **Multi-component data management** — register many components against a single job and let JobIt keep their data in sync
- **Execution order control** — assign priority to control which jobs execute when across the same frame
- **Safe memory management** — native containers are built, tracked, and disposed automatically; even handles destroyed objects mid-frame
- **Static scheduler facade** — one-line register/withdraw API from anywhere in your code

---

## What JobIt is Not

While powerful, JobIt is not a replacement for Unity's ECS system. Instead, it's designed as a halfway conversion, for projects that started in the classic `GameObject` flow, but now need the vast performance improvements that a DOTs approach gives. JobIt was originally built before the ECS system entered a production ready state, and now exists as a job wrapper for developers who cannot migrate their projects.

---

## Overview

Unity's Job System is powerful but verbose, with the ECS pattern being even more so. A typical workflow requires allocating `NativeArray`s, scheduling jobs with the right `JobHandle` dependencies, completing them at the right point in the frame, and cleaning everything up — for every job type you write. 

JobIt wraps that workflow into a simple class hierarchy:

1. You create a **job class** by inheriting from a base like `UpdateToUpdateJob<T>` and implementing your actual `IJobParallelFor` logic inside it.
2. You create a **data struct** `T` that describes what each registered component needs.
3. Any `MonoBehaviour` that wants to participate calls `UpdateJobScheduler.Register<MyJob, MyData>(this, data)` on `OnEnable` and `UpdateJobScheduler.Withdraw<MyJob, MyData>(this)` on `OnDisable`.

JobIt then:
- Keeps a `NativeContainer` buffer sized to all registered components
- Schedules your job every frame at the right point in the lifecycle
- Completes it before any code needs to read the results
- Disposes everything cleanly when the job is destroyed or Play Mode ends

---

## Requirements

| Dependency | Version |
|---|---|
| Unity | 2022.3.0f1 or newer |
| com.unity.burst | 1.8.9+ |
| com.unity.collections | 1.2.4+ |

---

## Installation

Open the Unity **Package Manager** (`Window → Package Manager`), click **+**, and choose **Add package from git URL**:

```
https://github.com/Reag/JobIt.git
```

Or add it directly to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.dms.jobit": "https://github.com/Reag/JobIt.git"
  }
}
```

---

## Usage

### 1. Define your data struct

This is the per-component data your job operates on.

```csharp
public struct MyJobData
{
    public float Speed;
    public float3 Direction;
}
```

### 2. Create your job class

Inherit from `UpdateToUpdateJob<T>` (schedules on `Update`, completes on `LateUpdate`) or `LateUpdateToLateUpdateJob<T>` (schedules before `LateUpdate` and completes near the end of `LateUpdate`).

Implement the abstract members to wire up your `NativeContainer`s and your actual `IJob` / `IJobParallelFor` struct.

```csharp
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using JobIt.Runtime.Abstract;

public class MoveJob : UpdateToUpdateJob<MyJobData>
{
    private NativeList<float> _speeds;
    private NativeList<float3> _directions;

    protected override void BuildNativeContainers()
    {
        _speeds     = new NativeList<float>(Allocator.Persistent);
        _directions = new NativeList<float3>(Allocator.Persistent);
    }

    protected override void DisposeLogic()
    {
        _speeds.Dispose();
        _directions.Dispose();
    }

    protected override void AddJobData(MyJobData data)
    {
        _speeds.Add(data.Speed);
        _directions.Add(data.Direction);
    }

    protected override void RemoveJobDataAndSwapBack(int index)
    {
        _speeds.RemoveAtSwapBack(index);
        _directions.RemoveAtSwapBack(index);
    }

    protected override MyJobData ReadJobDataAtIndex(int index) =>
        new MyJobData { Speed = _speeds[index], Direction = _directions[index] };

    // --- Job scheduling ---
    protected override JobHandle ScheduleJob(JobHandle dependsOn = default)
    {
        return new MoveParallelJob
        {
            Speeds     = _speeds.AsArray(),
            Directions = _directions.AsArray(),
            DeltaTime  = Time.deltaTime
        }.Schedule(JobSize, 64, dependsOn);
    }

    protected override void CompleteJob() { /* handled by invoker */ }

    [BurstCompile]
    struct MoveParallelJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float>  Speeds;
        [ReadOnly] public NativeArray<float3> Directions;
        public float DeltaTime;

        public void Execute(int index) 
        { 
        //Logic goes here
		}
    }
}
```

> See the **Inherit Transform** sample (`Window → Package Manager → JobIt → Samples`) for a complete, working example using `TransformAccessArray`.

### 3. Register components

Any `MonoBehaviour` that wants to participate adds itself to the job.

```csharp
using JobIt.Runtime.Impl.JobScheduler;
using UnityEngine;

public class Mover : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private Vector3 direction = Vector3.forward;

    private void OnEnable()
    {
        UpdateJobScheduler.Register<MoveJob, MyJobData>(
            this,
            new MyJobData { Speed = speed, Direction = direction }
        );
    }

    private void OnDisable()
    {
        UpdateJobScheduler.Withdraw<MoveJob, MyJobData>(this);
    }
}
```

That's it. The singleton job instance is created automatically the first time a component registers.

### 4. Update job data at runtime

Call `UpdateJobData` whenever the underlying values change.

```csharp
public void SetSpeed(float newSpeed)
{
    speed = newSpeed;
    UpdateJobScheduler.UpdateJobData<MoveJob, MyJobData>(
        this,
        new MyJobData { Speed = speed, Direction = direction }
    );
}
```

### 5. Read results back (optional)

Results written by the job into your `NativeContainer`s are available after the job completes. Use `TryReadItem` or subscribe to `OnJobComplete`.

```csharp
// Subscribe once (e.g. in OnEnable)
UpdateJobScheduler.GetJobObject<MoveJob, MyJobData>().OnJobComplete += OnMoveComplete;

private void OnMoveComplete()
{
    if (UpdateJobScheduler.TryReadJobData<MoveJob, MyJobData>(this, out var data))
    {
        // data is safe to read here
    }
}
```

---

## Lifecycle at a Glance

```
Update  ──► [UpdateJobInvoker]  Schedule all registered jobs
                                   (PreStartJob → ScheduleJob for each)
LateUpdate ──► [UpdateJobCompleter]  Complete all job handles
                                      → OnJobComplete fired
```

Swap `UpdateToUpdateJob` with `LateUpdateToLateUpdateJob` if your results are only needed within the same `LateUpdate`.

---

## Execution Order

When multiple jobs are registered with the same invoker, JobIt chains their `JobHandle`s in priority order. Assign a priority by overriding `JobPriority`:

```csharp
protected override int JobPriority => 10; // higher runs later
```

Jobs with the same priority receive a combined dependency handle (all of them must finish before the next priority tier begins).

---

## Samples

Import the **Inherit Transform** sample from the Package Manager to see a fully working job that parents one transform to another entirely through the Job System with Burst compilation.

---

## AI Discloser Statement

The only part of this library that involved the use of AI is this Readme. All of the actual code, tests, and structure are entirely created by humans with no AI assistance.

---
## License

MIT — see [LICENSE.md](LICENSE.md).