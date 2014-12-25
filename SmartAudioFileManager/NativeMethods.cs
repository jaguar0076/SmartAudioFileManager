using System.Runtime.InteropServices;

namespace SmartAudioFileManager
{
    internal static class NativeMethods
    {
        [DllImport("Kernel32", EntryPoint = "GetSystemPowerStatus")]
        internal static extern bool GetSystemPowerStatusRef(PowerState sps);
    }
}
