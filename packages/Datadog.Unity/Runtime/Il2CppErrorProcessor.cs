using System;
using System.Runtime.InteropServices;
using Datadog.Unity.Logs;

namespace Datadog.Unity
{
    class Il2CppErrorProcessor
    {
        private IDatadogPlatform _platform;
        private DdLogger _logger;

        public Il2CppErrorProcessor(IDatadogPlatform platform)
        {
            _platform = platform;
            _logger = DatadogSdk.Instance.CreateLogger(new ()
            {
                RemoteLogThreshold = DdLogLevel.Debug,
                Name = "IL2CPPLogger"
            });
        }

        /// <summary>
        /// Convert an exception to a native stack trace. If the stack trace cannot be converted,
        /// this returns exception.StackTrace.ToString().
        /// </summary>
        /// <param name="exception">The C# exception.</param>
        /// <returns>A string representation of the stack trace.</returns>
        public string GetNativeStackTrace(Exception exception)
        {
            var gchandle = GCHandle.Alloc(exception);
            var addresses = IntPtr.Zero;

            string resultStack = null;
            try
            {
                var handlePtr = GCHandle.ToIntPtr(gchandle);
                var targetAddress = GcHandleGetTarget(handlePtr);

                var numFrames = 0;
                string imageUuid = null;
                string imageName = null;
                GetStackFrames(targetAddress, out addresses, out numFrames, out imageUuid, out imageName);
                var frames = new IntPtr[numFrames];
                Marshal.Copy(addresses, frames, 0, numFrames);
                if (frames[0] == IntPtr.Zero)
                {
                    // First frame is null, this is likely a development build
                    return null;
                }

                resultStack = _platform.GetNativeStack(frames, imageUuid, imageName);
            }
            catch (Exception e)
            {
                _logger.Error($"Error getting native stack trace: {e}");
            }
            finally
            {
                gchandle.Free();
                if (addresses != IntPtr.Zero)
                {
                    il2cpp_free(addresses);
                }
            }

            return resultStack;
        }

        private void GetStackFrames(IntPtr exc, out IntPtr addresses, out int numFrames, out string imageUuid,
            out string imageName)
        {
            var imageUuidPtr = IntPtr.Zero;
            var imageNamePtr = IntPtr.Zero;
            try
            {
#if UNITY_2021_3_OR_NEWER
                il2cpp_native_stack_trace(exc, out addresses, out numFrames, out imageUuidPtr, out imageNamePtr);
                imageName = (imageNamePtr == IntPtr.Zero) ? null : Marshal.PtrToStringAnsi(imageNamePtr);
#else
                // Method expects a pre-allocated buffer for the UUID. Max length is 40 chars
                imageUuidPtr = il2cpp_alloc(41);
                il2cpp_native_stack_trace(exc, out addresses, out numFrames, imageUuidPtr);
                imageName = null;
#endif
                imageUuid = (imageUuidPtr == IntPtr.Zero) ? null : Marshal.PtrToStringAnsi(imageUuidPtr);
            }
            finally
            {
                il2cpp_free(imageUuidPtr);
                il2cpp_free(imageNamePtr);
            }
        }

        #region Unity C Interface

        private IntPtr GcHandleGetTarget(IntPtr gchandle)
        {
            #if UNITY_2023
            return il2cpp_gchandle_get_target(gchandle);
            #else
            return il2cpp_gchandle_get_target(gchandle.ToInt32());
            #endif
        }

#if UNITY_2023
        [DllImport("__Internal")]
        private static extern IntPtr il2cpp_gchandle_get_target(IntPtr gchandle);

#else // Pre Unity 2023
        [DllImport("__Internal")]
        private static extern IntPtr il2cpp_gchandle_get_target(int gchandle);
        #endif

#if UNITY_2021_3_OR_NEWER
        [DllImport("__Internal")]
        private static extern void il2cpp_native_stack_trace(IntPtr exc, out IntPtr addresses, out int numFrames, out IntPtr imageUUID, out IntPtr imageName);

#else
        [DllImport("__Internal")]
        private static extern IntPtr il2cpp_alloc(uint size);

        [DllImport("__Internal")]
        private static extern void il2cpp_native_stack_trace(IntPtr exc, out IntPtr addresses, out int numFrames, IntPtr imageUUID);
#endif

        [DllImport("__Internal")]
        private static extern void il2cpp_free(IntPtr ptr);

        #endregion
    }

}