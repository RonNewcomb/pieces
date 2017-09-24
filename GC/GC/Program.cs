using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;

public class Program
{
    static void Main(string[] args)
    {
        new Thread(Garbage.Collect) { Name = "garbage collection", IsBackground = true, Priority = ThreadPriority.BelowNormal }.Start();
    }
}

/// <summary> These 3 methods comprise the interface iGC, which the root ref object 'something' implements.</summary>
public static class/*interface*/ iGC
{
    public static bool apoptosis(this object me) { return true; }
    public static IEnumerable<object> allRefProperties(this object me) { yield return me; }
    public static object ValueOfProperty(this object my, object property) { return property; } // my.property
} // doesn't the value type 'Reference' need this?  it's a stand-alone pointer

/// <summary>An off-the-cuff garbage collector.</summary>
internal static class Garbage
{
    static hook clothesline = null; // a circularly-linked list (CCL) which always has myOwnHook in it
    static hook myOwnHook = new hook() { destroyMe = null, next = myOwnHook };   // This ever-present node in the CCL removes lots of edge cases.  
    static uint cycle;
    static object semaphore = new object();
    public static bool ProgramStillRunning = true;

    public static void Collect()
    {
		if (Garbage.clothesline != null) return; // there's only one GC, which never ends
		Garbage.clothesline = Garbage.myOwnHook;      // can't use static initializer cause of ordering issues
        hook previous = myOwnHook;
        for (hook current = myOwnHook; ProgramStillRunning || myOwnHook.next != myOwnHook; previous = current, current = current.next)
        {
            if (previous == myOwnHook || current == myOwnHook)
            {   // never operate on the myownhook--myownhook.next border, because that's where new items are added async-ly
                cycle++;
                continue;
            }
            if (current.destroyMe != null)
            {
                // call the dying object's destructor. If it doesn't want to be destroyed, (and it's young), skip it for now.
                if (!current.destroyMe.apoptosis())
                    if (cycle - current.age < 20)  
                        continue;
                // loop through all ref values held by the class and move them to the clothesline
                foreach (var property in current.destroyMe.allRefProperties())
                {
					Garbage.Add(current.destroyMe.ValueOfProperty(property));
                    //current.destroyMe.property = null;
                }
                // now remove this class from its hook
                memory.dealloc(current.destroyMe);
                current.destroyMe = null;
            }
            // now that the object is gone, remove its hook from the clothesline
            lock (semaphore)
                previous.next = current.next;
            current.next = null;
            // and destroy the hook itself
            memory.dealloc(current);
            // Set Current back to something on the clothesline.
            current = previous;
        }
    }

    /// <summary>Re-entrant. An object will soon be deconstructed by tossing it in here.</summary>
    public static void Add(object garbage)
    {
		var h = new hook { destroyMe = garbage, age = cycle };
		lock (semaphore)
		{
            // insert the new hook into the clothesline CLL by...the new hook points to whatever the GC's hook points to
			h.next = Garbage.myOwnHook.next;
			Garbage.myOwnHook.next = h;  // then the GC's hook points to the new object
        }
    }

}

/// <summary>Attaches a dying object to the clothesline; it is a node of the clothesline. </summary>
sealed class hook
{
	internal hook next;
	internal object destroyMe;
	internal uint age;
}

// stub.
public static class memory
{
    public static object alloc(int sizeInBytes)
    {
        return sizeInBytes + Marshal.SizeOf(typeof(hook));
    }
    public static void dealloc(object me)
    {
    }
}
