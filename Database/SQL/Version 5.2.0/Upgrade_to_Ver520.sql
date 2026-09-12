-- Upgrade_to_Ver520.sql - OpenSobek
--
-- Takes an existing 5.1.0 SQL Server database and brings it up to Version 5.2.0. If your
-- version is older than 5.1.0, run Upgrade_to_Ver510.sql first.


-- Splits signed GCS URL lifetimes by how the page uses the URL, instead of one lifetime for
-- everything. "GCS Signed URL Expiration Minutes" (default 240) keeps its value and now only
-- covers files the page keeps requesting while it is open (PDF, audio, video, page turner).
-- Page images and thumbnails the browser fetches immediately get "GCS Page Load URL Expiration
-- Minutes" (default 10), and links clicked later, such as the Downloads list, get "GCS Download
-- URL Expiration Minutes" (default 60). Restricted items never exceed "GCS Restricted URL
-- Expiration Minutes". Shorter lifetimes stop harvested or shared links from being reused.

	if ( NOT EXISTS (select 1 from SobekCM_Settings where Setting_Key = 'GCS Page Load URL Expiration Minutes' and Extension_Code is null))
	begin
		insert into dbo.SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, [Hidden], Reserved, Help )
		values ( 'GCS Page Load URL Expiration Minutes', '10', 'System / Server Settings', 'Server Settings', 0, 2, 'How long (in minutes) a signed URL stays valid for a GCS-hosted file the browser fetches immediately as the page renders, such as page images and thumbnails. Can be very short: nothing reuses the URL once the page has loaded, and a short value makes harvested or shared links stop working quickly. Restricted items never exceed GCS Restricted URL Expiration Minutes. Only used when File System Mode is "GCS Hybrid" or "GCS Full".' );
	end;

	if ( NOT EXISTS (select 1 from SobekCM_Settings where Setting_Key = 'GCS Download URL Expiration Minutes' and Extension_Code is null))
	begin
		insert into dbo.SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, [Hidden], Reserved, Help )
		values ( 'GCS Download URL Expiration Minutes', '60', 'System / Server Settings', 'Server Settings', 0, 2, 'How long (in minutes) a signed URL stays valid for a GCS-hosted file offered as a link the user clicks later, such as the Downloads list. Only needs to cover the time between the page loading and the click, since a download that has already started is not cut off when its URL expires. Restricted items never exceed GCS Restricted URL Expiration Minutes. Only used when File System Mode is "GCS Hybrid" or "GCS Full".' );
	end;

	update SobekCM_Settings
	set Help = 'How long (in minutes) a signed URL stays valid for a GCS-hosted file that keeps being requested while the page is open, such as PDFs, audio, video and the page turner, which make fresh requests as the user scrolls, seeks or turns pages. Page images and download links use the shorter GCS Page Load URL Expiration Minutes and GCS Download URL Expiration Minutes instead. Only used when File System Mode is "GCS Hybrid" or "GCS Full".'
	where Setting_Key = 'GCS Signed URL Expiration Minutes' and Extension_Code is null;
GO


/**************************************************************************/
/**                                                                      **/
/**   Update Database Version                                            **/
/**                                                                      **/
/**************************************************************************/

-- Update the version number
if (( select count(*) from SobekCM_Database_Version ) = 0 )
begin
	insert into SobekCM_Database_Version ( Major_Version, Minor_Version, Release_Phase )
	values ( 5, 2, '0' );
end
else
begin
	update SobekCM_Database_Version
	set Major_Version=5, Minor_Version=2, Release_Phase='0';
end;
GO
