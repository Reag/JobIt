using JobIt.Runtime.Utils;
using UnityEngine;

public class MatchPositionJobWrapper : JobWrapperBase<MatchPositionJob, MatchPositionJobElement>
{
    /// <inheritdoc />
    public MatchPositionJobWrapper(Component owner, MatchPositionJobElement data) : base(owner, data)
    {
    }
}