using MT.LightTask.Storage;
using System.Collections.Concurrent;

namespace MT.LightTask.Test.Web.Tasks
{
    public class JsonTaskStorageExtension : LightTaskFileStorage
    {
        public static Task Handler(IServiceProvider services, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
        private static readonly ConcurrentDictionary<string, Func<IServiceProvider, CancellationToken, Task>> caches = [];
        static JsonTaskStorageExtension()
        {
            caches["静态方法"] = Handler;
        }
        public override Func<IServiceProvider, CancellationToken, Task>? RestoreDelegateTask(string name)
        {
            caches.TryGetValue(name, out var func); 
            return func;
        }
    }
}
