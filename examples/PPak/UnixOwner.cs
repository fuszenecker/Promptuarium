using System.Runtime.InteropServices;

namespace PPak;

/// <summary>
/// The owner and the group of a file on Linux (.NET has no API for them): statx gives the ids, getpwuid and
/// getgrgid the names. Restoring them (lchown) works only for root.
/// </summary>
internal static class UnixOwner
{
    private const int AtFdCwd = -100;
    private const int AtSymlinkNoFollow = 0x100;
    private const uint StatxUid = 0x8;
    private const uint StatxGid = 0x10;

    /// <summary>The names of the owner and the group, or nulls if they are not known (or not on Linux).</summary>
    public static (string? Owner, string? Group) Get(string path)
    {
        if (!OperatingSystem.IsLinux())
        {
            return (null, null);
        }

        // struct statx is the same on every architecture: stx_uid at offset 20, stx_gid at 24
        byte[] buffer = new byte[256];

        if (statx(AtFdCwd, path, AtSymlinkNoFollow, StatxUid | StatxGid, buffer) != 0)
        {
            return (null, null);
        }

        uint uid = BitConverter.ToUInt32(buffer, 20);
        uint gid = BitConverter.ToUInt32(buffer, 24);

        // The first field of struct passwd and of struct group is the name
        return (NameAt(getpwuid(uid)), NameAt(getgrgid(gid)));
    }

    /// <summary>Sets the owner and the group (of the link itself, for a symbolic link), if the process is root.</summary>
    /// <returns>Whether they were set</returns>
    public static bool Set(string path, string? owner, string? group)
    {
        if (!OperatingSystem.IsLinux() || geteuid() != 0)
        {
            return false;
        }

        // The id follows the two pointers (name and password) in struct passwd and struct group
        uint uid = owner is not null && getpwnam(owner) is var user && user != IntPtr.Zero ? (uint)Marshal.ReadInt32(user, 2 * IntPtr.Size) : uint.MaxValue;
        uint gid = group is not null && getgrnam(group) is var grp && grp != IntPtr.Zero ? (uint)Marshal.ReadInt32(grp, 2 * IntPtr.Size) : uint.MaxValue;

        // -1 (uint.MaxValue) leaves an id unchanged
        return lchown(path, uid, gid) == 0;
    }

    private static string? NameAt(IntPtr entry) => entry == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(entry));

    [DllImport("libc", SetLastError = true)]
    private static extern int statx(int dirfd, [MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags, uint mask, byte[] buffer);

    [DllImport("libc")]
    private static extern IntPtr getpwuid(uint uid);

    [DllImport("libc")]
    private static extern IntPtr getgrgid(uint gid);

    [DllImport("libc")]
    private static extern IntPtr getpwnam([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport("libc")]
    private static extern IntPtr getgrnam([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport("libc", SetLastError = true)]
    private static extern int lchown([MarshalAs(UnmanagedType.LPUTF8Str)] string path, uint uid, uint gid);

    [DllImport("libc")]
    private static extern uint geteuid();
}
