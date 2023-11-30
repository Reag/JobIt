using JobIt.Runtime.Abstract;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;


public struct InheritTransformJobElement
{
    public Transform Parent;
    public Transform Child;
}

public class InheritTransformJob : UpdateToUpdateJob<InheritTransformJobElement>
{
    private TransformAccessArray _nativeChildren;
    private TransformAccessArray _nativeParents;
    private NativeList<Matrix4x4> _localToWorlds;

    private JobHandle _handle;

    protected override void BuildNativeContainers()
    {
        _nativeChildren = new TransformAccessArray(1000);
        _nativeParents = new TransformAccessArray(1000);
        _localToWorlds = new NativeList<Matrix4x4>(Allocator.Persistent);
    }

    protected override void DisposeLogic()
    {
        _handle.Complete();
        if (_nativeChildren.isCreated) _nativeChildren.Dispose();
        if (_nativeParents.isCreated) _nativeParents.Dispose();
        if (_localToWorlds.IsCreated) _localToWorlds.Dispose();
    }

    protected override JobHandle ScheduleJob(JobHandle dependsOn = default(JobHandle))
    {
        var load = new LoadParents {
            LocalToWorlds = _localToWorlds.AsArray()
        };
        _handle = load.ScheduleReadOnly(_nativeParents, 32, dependsOn);

        var job = new SetChildren {
            LocalToWorlds = _localToWorlds.AsArray()
        };
        _handle = job.Schedule(_nativeChildren, _handle);
        return _handle;
    }

    protected override void AddJobData(InheritTransformJobElement e)
    {
        if (e.Parent == null || e.Child == null) return;
        _localToWorlds.Add(Matrix4x4.identity);
        _nativeChildren.Add(e.Child);
        _nativeParents.Add(e.Parent);
    }

    protected override void RemoveJobDataAndSwapBack(int index)
    {
        _localToWorlds.RemoveAtSwapBack(index);
        _nativeChildren.RemoveAtSwapBack(index);
        _nativeParents.RemoveAtSwapBack(index);
    }

    protected override InheritTransformJobElement ReadJobDataAtIndex(int index)
    {
        var ret = new InheritTransformJobElement {
            Parent = _nativeParents[index],
            Child = _nativeChildren[index]
        };
        return ret;
    }

    [BurstCompile(OptimizeFor = OptimizeFor.Performance, CompileSynchronously = true)]
    private struct LoadParents : IJobParallelForTransform
    {
        public NativeArray<Matrix4x4> LocalToWorlds;
        public void Execute(int index, TransformAccess transform)
        {
            if (transform.isValid)
                LocalToWorlds[index] = transform.localToWorldMatrix;
            else
                LocalToWorlds[index] = Matrix4x4.zero;
        }
    }

    [BurstCompile(OptimizeFor = OptimizeFor.Performance, CompileSynchronously = true)]
    private struct SetChildren : IJobParallelForTransform
    {
        public NativeArray<Matrix4x4> LocalToWorlds;

        public void Execute(int index, TransformAccess transform)
        {
            if (!transform.isValid || LocalToWorlds[index] == Matrix4x4.zero) return;
            transform.localPosition = LocalToWorlds[index].GetPosition();
            transform.localRotation = LocalToWorlds[index].rotation;
            transform.localScale = LocalToWorlds[index].lossyScale;
        }
    }
}