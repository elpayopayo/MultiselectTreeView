using System;
using System.Threading;
using System.Threading.Tasks;

namespace WpfMultiselectTreeViewKit.Utils
{
    	
public static class AsyncHelper
{
    private static TaskFactory Factory
    {
        get
        {
            return smTaskFactory ?? (smTaskFactory = new
                TaskFactory(CancellationToken.None,
                    TaskCreationOptions.None,
                    TaskContinuationOptions.None,
                    TaskScheduler.Default));
        }
    }

    private static TaskFactory smTaskFactory;

    public static TResult RunSync<TResult>(Func<Task<TResult>> func)
    {
        return Factory
          .StartNew(func)
          .Unwrap()
          .GetAwaiter()
          .GetResult();
    }

    public static void RunSync(Func<Task> func)
    {
        Factory
          .StartNew(func)
          .Unwrap()
          .GetAwaiter()
          .GetResult();
    }
}
}
