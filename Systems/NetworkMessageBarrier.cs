
using Unity.Entities;

[UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate.ScriptRunBehaviourFixedUpdate))]
[UpdateBefore(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate.DirectorFixedUpdate))]
public class NetworkMessageBarrier : BarrierSystem { }
