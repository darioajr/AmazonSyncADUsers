using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.ServiceModel;

namespace AmazonSyncADUsers
{
    public partial class Service1 : ServiceBase
    {
        private int _TimeSync = int.Parse(System.Configuration.ConfigurationManager.AppSettings["TimeSync"].ToString());

        // Event batch execution
        public System.Timers.Timer _aTimer;

        public Service1()
        {
            InitializeComponent();

            // Create eventlog source
            if (!System.Diagnostics.EventLog.SourceExists("AmazonSyncADUsers"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "AmazonSyncADUsers", "AmazonSyncADUsersLog");
            }

            string log = System.Diagnostics.EventLog.LogNameFromSourceName("AmazonSyncADUsers", ".");

            eventLog1.Source = "AmazonSyncADUsers";
            eventLog1.Log = log;

        }

        protected override void OnStart(string[] args)
        {
            Start();
        }

        public void Start()
        {
            try
            {
                // Register info in eventlog
                eventLog1.WriteEntry("In OnStart");

                // Init the timer
                _aTimer = new System.Timers.Timer();
                _aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
                _aTimer.Interval = _TimeSync;
                _aTimer.Enabled = true;
            }
            catch (Exception e)
            {
                eventLog1.WriteEntry("OnStart Exception = " + e.Message, EventLogEntryType.Error);
                throw new System.Exception(e.Message);
            }
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            // For security, STOP the timer
            _aTimer.Stop();

            try
            {
                var users = new List<string>();

                // List AD users that in the group
                ActiveDirectoryHelper.GetAllUsersOnGroup(users);

                // Remove access from users in IAM that don´t in the AD group
                AmazonS3Helper.RemoveUsersNotIn(users);

                // Grant access to new users in the AD group
                AmazonS3Helper.CreateUsers(users);

                // Restart the timer
                _aTimer.Start();
            }
            catch (Exception ex)
            {
                // Write error on the eventos
                eventLog1.WriteEntry("OnTimedEvent Exception = " + ex.Message, EventLogEntryType.Error);

                // Restart the timer
                _aTimer.Start();
            }
        }
        protected override void OnStop()
        {
            Stop();
        }

        protected void Stop()
        {
            _aTimer.Stop();
            _aTimer.Dispose();

            eventLog1.WriteEntry("In OnStop");
        }
    }
}
