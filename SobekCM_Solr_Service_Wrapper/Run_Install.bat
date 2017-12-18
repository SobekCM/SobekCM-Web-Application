C:
CD "D:\SolrMonitor"
C:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil D:\SolrMonitor\SolrServiceMonitor.exe
PAUSE
eventcreate /ID 1 /L APPLICATION /T INFORMATION /SO "Solr Monitoring Service" /D "Setting up source for event logs"
PAUSE
