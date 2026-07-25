using System.ServiceProcess;

namespace ConsoleApplication1
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            //components = new System.ComponentModel.Container();

            this.Solr_Monitor_Service_ProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.Solr_Monitor_Service_Installer = new System.ServiceProcess.ServiceInstaller();
            // 
            // Web_Ingest_Service_ProcessInstaller
            // 
            this.Solr_Monitor_Service_ProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.Solr_Monitor_Service_ProcessInstaller.Password = null;
            this.Solr_Monitor_Service_ProcessInstaller.Username = null;
            // 
            // Web_Ingest_Service_Installer
            // 
            this.Solr_Monitor_Service_Installer.ServiceName = "Solr Monitor Service";
            this.Solr_Monitor_Service_Installer.DisplayName = "Solr Monitor Service";
            this.Solr_Monitor_Service_Installer.Description = "Service starts and stops solr/lucene, monitors via solr 'pings', and allows a monitoring port to be opened for external health check queries as well.";
            this.Solr_Monitor_Service_Installer.StartType = ServiceStartMode.Automatic;
            this.Solr_Monitor_Service_Installer.DelayedAutoStart = true;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[]
            {
                this.Solr_Monitor_Service_ProcessInstaller,
                this.Solr_Monitor_Service_Installer
            });
        }

        #endregion

        public System.ServiceProcess.ServiceProcessInstaller Solr_Monitor_Service_ProcessInstaller;
        public System.ServiceProcess.ServiceInstaller Solr_Monitor_Service_Installer;
    }
}