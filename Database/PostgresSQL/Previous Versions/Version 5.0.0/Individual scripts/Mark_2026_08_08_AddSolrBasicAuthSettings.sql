	insert into SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options )
	values ( 'Solr Username', '', 'System / Server Settings', 'Search Preferences', false, 2, 'Username for HTTP Basic Authentication against the Solr document and page indexes, if the Solr instance requires it.  Leave blank if Solr does not require authentication.', NULL );

	insert into SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options )
	values ( 'Solr Password', '', 'System / Server Settings', 'Search Preferences', false, 2, 'Password for HTTP Basic Authentication against the Solr document and page indexes, if the Solr instance requires it.  Leave blank if Solr does not require authentication.', NULL );

