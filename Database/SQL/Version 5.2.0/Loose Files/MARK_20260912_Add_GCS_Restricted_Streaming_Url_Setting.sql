-- Gives restricted items a separate, longer cap for files the page keeps requesting (PDF, audio,
-- video). Before this, every signed URL on a restricted item was capped at "GCS Restricted URL
-- Expiration Minutes" (default 15), so an authorized user seeking or scrolling past 15 minutes
-- hit an expired URL. That setting now caps only page images, thumbnails and download links.

	if ( NOT EXISTS (select 1 from SobekCM_Settings where Setting_Key = 'GCS Restricted Streaming URL Expiration Minutes' and Extension_Code is null))
	begin
		insert into dbo.SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, [Hidden], Reserved, Help )
		values ( 'GCS Restricted Streaming URL Expiration Minutes', '60', 'System / Server Settings', 'Server Settings', 0, 2, 'How long (in minutes) a signed URL stays valid for a file on an IP- or user-group-restricted (but not dark) item that keeps being requested while the page is open, such as PDFs, audio and video, which make fresh requests as the user scrolls or seeks. Longer than GCS Restricted URL Expiration Minutes because a shorter value breaks playback and reading partway through. Never exceeds GCS Signed URL Expiration Minutes. Only used when File System Mode is "GCS Hybrid" or "GCS Full".' );
	end;

	update SobekCM_Settings
	set Help = 'How long (in minutes) a signed URL stays valid for a page image, thumbnail or download link on an IP- or user-group-restricted (but not dark) item. Deliberately much shorter than the public lifetimes, since a signed URL is a bearer token that works for anyone holding it once handed out. Files the page keeps requesting, such as PDFs, audio and video, use GCS Restricted Streaming URL Expiration Minutes instead. Only used when File System Mode is "GCS Hybrid" or "GCS Full".'
	where Setting_Key = 'GCS Restricted URL Expiration Minutes' and Extension_Code is null;
