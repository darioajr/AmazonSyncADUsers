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

        //evento de execução em lote
        public System.Timers.Timer _aTimer;

        public Service1()
        {
            InitializeComponent();

            //registra no eventlog
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
            //inicializa o sistema de processamento
            Iniciar();
        }

        public void Iniciar()
        {
            try
            {
                //grava log
                eventLog1.WriteEntry("In OnStart");

                //inicia evento de processamento das mensagens
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
            //por segurança, PARA o evento, pois este processo pode demorar,
            //e não podemos correr o risco de executar em paralelo
            _aTimer.Stop();

            try
            {
                var users = new List<string>();

                //lista usuarios do AD que tem acesso ao grupo da amazon
                ActiveDirectoryHelper.GetAllUsersOnGroup(users);

                //remove acesso dos usuarios na amazon que nao estejam no grupo do AD
                AmazonS3Helper.RemoveUsersNotIn(users);

                //concede acesso a novos usuarios do AD no grupo amazon
                AmazonS3Helper.CreateUsers(users);

                //reinicia o processamento de próximos registros
                _aTimer.Start();
            }
            catch (Exception ex)
            {
                //grava informações no log de eventos do windows
                eventLog1.WriteEntry("OnTimedEvent Exception = " + ex.Message, EventLogEntryType.Error);

                //reinicia o processamento de próximos registros
                _aTimer.Start();
            }
        }
        protected override void OnStop()
        {
            Parar();
        }

        protected void Parar()
        {
            _aTimer.Stop();
            _aTimer.Dispose();

            eventLog1.WriteEntry("In OnStop");
        }
    }
}
