using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Cat
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Check to see if there is already another CAT process open
                if (ProcessIsOpen("Cat"))
                {
                    MessageBox.Show("There is already an instance of SmartSDR Cat running.", "FlexRadio Systems CAT: Already Running", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Add the event handler for handling UI thread exceptions to the event.
                Application.ThreadException += new ThreadExceptionEventHandler(UIThreadException);

                // Set the unhandled exception mode to force all Windows Forms errors to go through
                // our handler.
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

                // Add the event handler for handling non-UI thread exceptions to the event. 
                AppDomain.CurrentDomain.UnhandledException +=
                    new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

                Application.Run(new Main());
            }
            catch (Exception ex)
            {
                string error_msg = ex.Message + "\n\n" + ex.StackTrace;
                if (ex.InnerException != null)
                    error_msg += "\n\n" + ex.InnerException.Message;

                HandleException(error_msg);
            }
        }

        private static void UIThreadException(object sender, ThreadExceptionEventArgs t)
        {
            string error_msg = t.Exception.Message + "\n\n";
            if (t.Exception.InnerException != null)
                error_msg += t.Exception.InnerException.Message + "\n\n";
            error_msg += t.Exception.StackTrace;

            HandleException(error_msg);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Exception ex = (Exception)e.ExceptionObject;
                string error_msg = ex.Message + "\n\n";
                if (ex.InnerException != null)
                    error_msg += ex.InnerException.Message + "\n\n";
                error_msg += ex.StackTrace;

                HandleException(error_msg);
            }
            catch (Exception exc)
            {
                MessageBox.Show("Fatal Non-UI Error",
                    "Fatal Non-UI Error.  Could not write the error to the event log.  Reason: "
                    + exc.Message, MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        private static void HandleException(string message)
        {
            MessageBox.Show(message, "SmartSDR Cat: Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

        }


        private static bool ProcessIsOpen(String process)
        {
            // find all open DAX processes
            Process[] p = Process.GetProcessesByName(process);
            if (p.Length > 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
