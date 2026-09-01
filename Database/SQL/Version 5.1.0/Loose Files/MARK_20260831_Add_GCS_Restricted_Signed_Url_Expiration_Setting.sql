-- Adds the setting for how long a signed URL stays valid for a file on an IP- or
-- user-group-restricted (but not dark) item, separate from the existing "GCS Signed
-- URL Expiration Minutes" (which is now only used for public items). A signed URL is
-- a bearer token with no per-user binding -- once handed to an authorized viewer's
-- browser, it works for anyone holding it until it expires, regardless of who actually
-- requests it. A short expiration is the compensating control for that gap. Every
-- existing deployment defaults to 15 minutes and is unaffected until an admin changes it.

	insert into dbo.SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help )
	values ( 'GCS Restricted Signed URL Expiration Minutes', '15', 'System / Server Settings', 'Server Settings', 0, 2, 'How long (in minutes) a signed URL stays valid for a file on an IP- or user-group-restricted (but not dark) item. Deliberately much shorter than GCS Signed URL Expiration Minutes, since a signed URL is a bearer token that works for anyone holding it once handed out. Only used when File System Mode is "GCS Hybrid" or "GCS Full".' );
