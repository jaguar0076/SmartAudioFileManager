using System.Runtime.InteropServices;

namespace SmartAudioFileManager
{   
    //Class NativeMethods, all P/Invoke should ne declared here

    internal static class NativeMethods
    {
        //return system power state information
        [DllImport("Kernel32", EntryPoint = "GetSystemPowerStatus")]
        internal static extern bool GetSystemPowerStatusRef(PowerState sps);
    }
}
