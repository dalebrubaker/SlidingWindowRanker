using System.Runtime.InteropServices;

namespace SlidingWindowRanker;

public class SafeUnmanagedMemoryHandle : SafeHandle
{
    // Constructor initializes the handle
    public SafeUnmanagedMemoryHandle(int size) : base(IntPtr.Zero, true)
    {
        // Allocate unmanaged memory
        SetHandle(Marshal.AllocHGlobal(size));
    }

    // Override IsInvalid to determine if the handle is valid
    public override bool IsInvalid => handle == IntPtr.Zero;

    // Override ReleaseHandle to free the unmanaged memory
    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            Marshal.FreeHGlobal(handle);
            handle = IntPtr.Zero;
        }
        return true;
    }
}