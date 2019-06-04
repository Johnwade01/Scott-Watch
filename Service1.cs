using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.IO;
using IBM.Data.DB2.iSeries;
using System.Configuration;


namespace ScottWatch
{
    public partial class Service1 : ServiceBase
    {
        string AppDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public Service1()
        {
            InitializeComponent();
        }
        private System.Timers.Timer m_mainTimer;
        private bool m_timerTaskSuccess;

        private System.Timers.Timer pm_mainTimer;
        private bool pm_timerTaskSuccess;

        protected override void OnStart(string[] args)
        {
            try
            {
                
                //
                // Create and start a timer for Pick And Puts.
                //
                int TimerInterval = Int32.Parse(ConfigurationManager.AppSettings["TimerInterval"]);
                File.AppendAllText(AppDir + @"\ServiceLogPP-" + DateTime.Now.ToString("MM-dd-yyyy") + ".txt", Environment.NewLine + "Pick/Put Service Started: " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss") + " - Interval:" + TimerInterval.ToString());
                m_mainTimer = new System.Timers.Timer();
                m_mainTimer.Interval = TimerInterval;   
                m_mainTimer.Elapsed += m_mainTimer_Elapsed;
                m_mainTimer.AutoReset = false;  // makes it fire only once
                m_mainTimer.Start(); // Start

                m_timerTaskSuccess = false;

                //
                // Create and start a timer for PartsMaster.
                //
                int PMTimerInterval = Int32.Parse(ConfigurationManager.AppSettings["PMTimerInterval"]);
                File.AppendAllText(AppDir + @"\ServiceLogPP-" + DateTime.Now.ToString("MM-dd-yyyy") + ".txt", Environment.NewLine + "PartsMater Service Started: " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss") + " - Interval:" + PMTimerInterval.ToString());
                pm_mainTimer = new System.Timers.Timer();
                pm_mainTimer.Interval = PMTimerInterval;   
                pm_mainTimer.Elapsed += pm_mainTimer_Elapsed;
                pm_mainTimer.AutoReset = false;  // makes it fire only once
                pm_mainTimer.Start(); // Start

                pm_timerTaskSuccess = false;



            }
            catch (Exception ex)
            {
                File.AppendAllText(AppDir + @"\ServiceLogPP-" + DateTime.Now.ToString("MM-dd-yyyy") + ".txt", Environment.NewLine + "Service Start Error: " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss") + ex.Message);
            }
        }

        protected override void OnStop()
        {
            try
            {
                File.AppendAllText(AppDir + @"\ServiceLogPP-" + DateTime.Now.ToString("MM-dd-yyyy") + ".txt", Environment.NewLine + "Service Stopped : " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss"));
                // Service stopped. Also stop the timer.
                m_mainTimer.Stop();
                m_mainTimer.Dispose();
                m_mainTimer = null;

                pm_mainTimer.Stop();
                pm_mainTimer.Dispose();
                pm_mainTimer = null;

            }
            catch (Exception ex)
            {
                File.AppendAllText(AppDir + @"\ServiceLogPP-" + DateTime.Now.ToString("MM-dd-yyyy") + ".txt", Environment.NewLine + "Service Stop Error: " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss") + ex.Message);
            }
        }
        void m_mainTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                // do some work
                File.AppendAllText(AppDir + @"\ServiceLogPP-" + DateTime.Now.ToString("MM-dd-yyyy") + ".txt", Environment.NewLine + "Service CheckForNewData - " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss"));
                string result = DBUtils.CheckForNewData();
                File.AppendAllText(AppDir + @"\ServiceLogPP-" + DateTime.Now.ToString("MM-dd-yyyy") + ".txt", Environment.NewLine + "CheckForNewData - " + result + " - "+ DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss")) ;
                m_timerTaskSuccess = true;
                
            }
            catch (Exception ex)
            {
                m_timerTaskSuccess = false;
                File.AppendAllText(AppDir + @"\ServiceLogPP-" + DateTime.Now.ToString("MM-dd-yyyy") + ".txt", Environment.NewLine + "Service CheckForNewData Error: " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss") + ex.Message);
            }
            finally
            {
                if (m_timerTaskSuccess)
                {
                    m_mainTimer.Start();
                }
            }
        }

        void pm_mainTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                // do some work
                File.AppendAllText(AppDir + @"\ServiceLogPM-" + DateTime.Now.ToString("MM-dd-yyyy") + ".txt", Environment.NewLine + "Service CheckForNewDataPM - " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss"));
                string result = DBUtils.CheckForNewData_PM();
                File.AppendAllText(AppDir + @"\ServiceLogPM-" + DateTime.Now.ToString("MM-dd-yyyy") + ".txt", Environment.NewLine + "CheckForNewDataPM - " + result + " - " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss"));
                pm_timerTaskSuccess = true;

            }
            catch (Exception ex)
            {
                pm_timerTaskSuccess = false;
                File.AppendAllText(AppDir + @"\ServiceLogPM-" + DateTime.Now.ToString("MM-dd-yyyy") + ".txt", Environment.NewLine + "Service CheckForNewDataPM Error: " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss") + ex.Message);
            }
            finally
            {
                if (pm_timerTaskSuccess)
                {
                    pm_mainTimer.Start();
                }
            }
        }

    }
}
