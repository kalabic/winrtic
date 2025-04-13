using System.Diagnostics;

namespace OpenRTIC.MiniTaskLib;

public class TaskTool
{
    static public long CancelStopDisposeAll(List<TaskWithEvents> taskList, int timeoutMs = -1)
    {
        var runningTasks = new List<TaskWithEvents>();
        foreach (var task in taskList)
        {
            if (!task.IsCompleted)
            {
                runningTasks.Add(task);
            }
            else
            {
                task.Dispose();
            }
        }

        if (runningTasks.Count > 0)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var stopCanceler = new CancellationTokenSource();
            var taskStoppersArray = new TaskWithEvents[runningTasks.Count];
            for (int index = 0; index < runningTasks.Count; index++)
            {
                taskStoppersArray[index] = runningTasks[index].StopDeviceTask(stopCanceler.Token);
            }
            bool result = Task.WaitAll(taskStoppersArray, timeoutMs);
            stopwatch.Stop();
            if (!result)
            {
                stopCanceler.Cancel();
                return -1;
            }
            return stopwatch.ElapsedMilliseconds;
        }

        return 0;
    }
}
