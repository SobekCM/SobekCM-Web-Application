	insert into SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options )
	values ( 'Forward to Https', 'false', 'System / Server Settings', 'Server Settings', false, 2, 'Flag indicates whether requests to the application (excluding static file requests, such as images, css, and js) arriving over HTTP should be redirected to the HTTPS version of the same URL.', 'true|false' );

