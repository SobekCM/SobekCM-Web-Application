-- Adds the three settings needed to enable "GCS Hybrid" file system mode: which mode is
-- active, which GCS bucket master files go to, and how long signed URLs to GCS-hosted
-- files remain valid. The service-account JSON key file path is deliberately NOT a
-- setting -- it's a fixed convention off Base_Directory (config\gcs-service-account.json),
-- kept out of the DB since it's a much higher-stakes credential than the other settings
-- stored here. Every existing deployment defaults to 'Local' and is unaffected until an
-- admin explicitly switches File System Mode.

	insert into dbo.SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options )
	values ( 'File System Mode', 'Local', 'System / Server Settings', 'Server Settings', 0, 2, 'Determines where digital resource files are stored/served from. "Local" uses the on-disk pairtree structure. "GCS Hybrid" stores master image files in Google Cloud Storage while keeping METS/marc.xml/thumbnails locally as well.', 'Local|GCS Hybrid' );

	insert into dbo.SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help )
	values ( 'GCS Bucket Name', '', 'System / Server Settings', 'Server Settings', 0, 2, 'Name of the Google Cloud Storage bucket used when File System Mode is "GCS Hybrid".' );

	insert into dbo.SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help )
	values ( 'GCS Signed URL Expiration Minutes', '240', 'System / Server Settings', 'Server Settings', 0, 2, 'How long (in minutes) a signed URL to a GCS-hosted file stays valid before expiring. Only used when File System Mode is "GCS Hybrid".' );

