#if !UNITY_6000_5_OR_NEWER
using System;

// Unity.Scripting.LifecycleManagement is only available from Unity 6000.5. This package supports older
// versions, so provide no-op stand-ins to let the attributes compile everywhere. On 6000.5+ the real ones are used.
namespace Unity.Scripting.LifecycleManagement
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event)]
    internal sealed class NoAutoStaticsCleanupAttribute : Attribute
    {
    }
}
#endif
