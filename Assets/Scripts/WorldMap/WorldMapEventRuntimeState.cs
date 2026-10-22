using UnityEngine;

// Transient handoff for the next world-map visit, never a scene-entry default.
public static class WorldMapEventRuntimeState
{
    static WorldMapEventData pending;
    public static void Queue(WorldMapEventData data) { if(data!=null)pending=data; }
    public static WorldMapEventData Consume() { var data=pending;pending=null;return data; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Clear() { pending=null; }
}
