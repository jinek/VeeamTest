using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ZipZip.Exceptions
{
    public static class ExceptionManager
    {
        static ExceptionManager()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            //We don't use tasks, but .NET uses itself heavily. We are still responsible for exceptions in application
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
        }

        public static void CatchExceptionsFromNowAndForever()
        {
            //it's just for calling the constructor, which is thread safe and will be called once only
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ProcessException((Exception) e.ExceptionObject);
        }

        private static void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            if (e.Observed) return;

            foreach (Exception exception in e.Exception.InnerExceptions)
            {
                if (Debugger.IsAttached) Debugger.Break(); //Exception in un-awaited task happened, please check it

                ProcessException(exception);
            }
        }

        private static void ProcessException(Exception exception)
        {
            switch (exception)
            {
                case UserErrorException userErrorException:
                    SayConsoleErrorAndExit(userErrorException.Message);
                    ApplicationExit();
                    break;
                default:
                    if (!IsDebug)
                        // ReSharper disable HeuristicUnreachableCode
#pragma warning disable 162
                        //todo: Add any kind of exception tracking, for example write to Windows Events and use Excellence Program or use Azure App Insights, DataDog etc 
                        SayConsoleErrorAndExit(
                            "Error happened. We have already started working on this issue. Please, accept our apologizes.");
#pragma warning restore 162
                    // ReSharper restore HeuristicUnreachableCode
                    break;
            }
        }

        private static void SayConsoleErrorAndExit(string message)
        {
            Console.WriteLine(message);
            Console.Error.WriteLine("ERROR: " + message);

            //to prevent application from being debugged or standard windows error message be shown
            ApplicationExit();
        }

        private static void ApplicationExit()
        {
            Environment.Exit(1);
        }

        private const bool IsDebug =
#if DEBUG
                true
#else
                false
#endif
            ;
    }
}

#if DEBUG
#warning WARNING!!!!!!!!!!!!!!!!!!   WARNING!!!!!!!!!!!!!!!!!!!!!!   In DEBUG mode application does not behave as stated in task. Switch to RELEASE mode. (DEBUG does not react to unhandled exceptions)
#endif