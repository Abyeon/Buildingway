using System;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;

namespace Buildingway.Utils;

public static class FrameworkExtensions
{
    public static async Task<T?> PollForValue<T>(this IFramework framework, Func<T> poll, Func<T, bool> predicate, int intervalMs = 0, int timeoutMs = 5000)
    {
        DateTime startTime = DateTime.Now;

        while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
        {
            T? result = default;
            
            await framework.RunOnFrameworkThread(() =>
            {
                result = poll();
            });

            if (result != null && predicate(result))
            {
                return result;
            }
            
            await Task.Delay(intervalMs).ConfigureAwait(false);
        }
        
        // Timeout, return default;
        return default(T);
    }

    public static async void RunForLength(this IFramework framework, Action action, int timeoutMs)
    {
        try
        {
            DateTime startTime = DateTime.Now;

            while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
            {
                await framework.RunOnFrameworkThread(action);
            }
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e.ToString());
        }
    }
}
