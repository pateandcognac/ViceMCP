using System.Net.Sockets;

namespace System
{
    internal static class SystemExtension
    {
        internal static byte AsByte(this bool value) => value ? (byte)1 : (byte)0;
        internal static Task WaitForDataAsync(this Socket socket, CancellationToken ct = default)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            
            // Register cancellation
            var registration = ct.Register(() => tcs.TrySetCanceled(ct));
            
            try
            {
                socket.BeginReceive([], 0, 0, SocketFlags.Peek, ar =>
                {
                    try
                    {
                        socket.EndReceive(ar);
                        tcs.TrySetResult();
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                    finally
                    {
                        registration.Dispose();
                    }
                }, null);
            }
            catch (Exception ex)
            {
                registration.Dispose();
                tcs.TrySetException(ex);
            }
            
            return tcs.Task;
        }
    }
}