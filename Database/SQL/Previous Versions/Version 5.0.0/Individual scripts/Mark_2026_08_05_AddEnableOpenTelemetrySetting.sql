	insert into dbo.SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options )
	values ( 'Enable OpenTelemetry', 'false', 'System / Server Settings', 'Server Settings', 0, 2, 'Flag indicates whether OpenTelemetry instrumentation (tracing) should be enabled for this instance.  The OTLP collector endpoint itself is configured separately, in appsettings.json.', 'true|false' );

