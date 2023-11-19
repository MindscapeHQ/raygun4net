using System;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net
{
  internal class Fire
  {

    /// <summary>
    /// If you need to see the details of an exception thrown please, add a logging action here.
    /// </summary>
    internal static Action<string> DebugLog { get; set; } = message => { /* NOOP by default */};

    /// <summary>
    /// If you want to return as soon as possible (and not block or await)
    /// Pass in an action into this.
    /// </summary>
    /// <remarks>
    /// This method will swallow *all* exceptions thrown.
    /// I am not your therapist. Please deal with your own exceptions</remarks>
    /// <param name="action"></param>
    /// <param name="memberName"></param>
    /// <param name="sourceLineNumber"></param>
    internal static void AndForget(
      Action action,
      [System.Runtime.CompilerServices.CallerMemberName]
      string memberName = "",
      [System.Runtime.CompilerServices.CallerLineNumber]
      int sourceLineNumber = 0)
    {
      Task ActionAsTask()
      {
        action();
        return Task.CompletedTask;
      }

      AndForget(ActionAsTask,
        memberName,
        // ReSharper disable once ExplicitCallerInfoArgument
        sourceLineNumber
        );
    }

    /// <summary>
    /// If you want to return as soon as possible (and not block or await)
    /// Pass in an action into this.
    /// </summary>
    /// <remarks>
    /// This method will swallow *all* exceptions thrown.
    /// I am not your therapist. Please deal with your own exceptions</remarks>
    /// <param name="task"></param>
    /// <param name="memberName"></param>
    /// <param name="sourceLineNumber"></param>
    internal static void AndForget(
      Task task,
      [System.Runtime.CompilerServices.CallerMemberName]
      string memberName = "",
      [System.Runtime.CompilerServices.CallerLineNumber]
      int sourceLineNumber = 0)
    {

      AndForget(() => task,
        memberName,
        // ReSharper disable once ExplicitCallerInfoArgument
        sourceLineNumber
        );
    }

    /// <summary>
    /// If you want to return as soon as possible (and not block or await)
    /// Pass in an action inside this
    /// </summary>
    /// <remarks>
    /// This method will swallow *all* exceptions thrown.
    /// I am not your therapist. Please deal with your own exceptions</remarks>
    /// <param name="action"></param>
    /// <param name="memberName"></param>
    /// <param name="sourceLineNumber"></param>
    internal static void AndForget(
      Func<Task> action,
      [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
      [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
    {
      try
      {
        //Explicitly discard (`_ = `) the Task. It will continue running in the background.
        _ = Task.Run(async () =>
        {
          try
          {
            await action();
          }
          catch (Exception ex) // catch internal errors
          {

            var message = $@"
                Error trying to perform action. 
                Message: {ex.Message}
                Called from {memberName}: {sourceLineNumber} ";

            DebugLog(message);

            /* Swallow! */
          }

        });

      }
      catch (Exception ex) // catch Task.Run errors
      {
        var message = $@"
            Error trying to  run task in background. 
            Message: {ex.Message}
            Called from {memberName}: {sourceLineNumber} ";

        DebugLog(message);

        /* Swallow! */
      }
    }

  }
}