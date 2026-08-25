-- Adds the two Builder modules needed for "GCS Hybrid" file system mode: staging existing
-- GCS-hosted files back to local disk before reprocessing an item, and pushing master
-- image files to GCS (deleting the local scratch copy) once the rest of the item's
-- processing has finished. Both modules self-no-op when File System Mode is "Local", so
-- these rows are safe to enable unconditionally in every deployment.

if (( select count(*) from SobekCM_Builder_Module where [Class]='SobekCM.Builder_Library.Modules.Items.StageResourceFilesLocallyModule') = 0)
begin
  insert into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, Class, [Enabled], [Order])
  values (3, 'Stage existing GCS-hosted files locally before reprocessing, in GCS Hybrid mode', 'SobekCM.Builder_Library.Modules.Items.StageResourceFilesLocallyModule', 'true', 5);
end;
GO

if (( select count(*) from SobekCM_Builder_Module where [Class]='SobekCM.Builder_Library.Modules.Items.PushMasterFilesToGcsModule') = 0)
begin
  insert into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, Class, [Enabled], [Order])
  values (3, 'Upload master/derivative image files to GCS and remove the local scratch copy, in GCS Hybrid mode', 'SobekCM.Builder_Library.Modules.Items.PushMasterFilesToGcsModule', 'true', 270);
end;
GO


-- Adds per-extension settings storage, reusing the existing generic SobekCM_Settings
-- key/value table rather than a new table. Extension_Code is nullable so every existing
-- row (Extension_Code IS NULL) is unaffected. Extension-owned rows use namespaced
-- Setting_Key values (e.g. 'OIDC|{Provider_Code}|ClientSecret') so a customer can
-- eventually run more than one instance of the same provider type.
alter table dbo.SobekCM_Settings add Extension_Code nvarchar(50) NULL;
GO

-- Gets all the settings rows belonging to a single extension, by extension code.
-- Deliberately a sibling of SobekCM_Get_Settings, not a modification of it, so the
-- existing settings loader (which pulls every unfiltered row) is unaffected.
CREATE PROCEDURE [dbo].[SobekCM_Get_Extension_Settings]
	@Extension_Code nvarchar(50)
AS
BEGIN

	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select Setting_Key, Setting_Value
	from SobekCM_Settings
	where Extension_Code = @Extension_Code;

END;
GO

-- Sets a single setting value scoped to one extension, by extension code and key.
-- Adds a new row if this is a new key for that extension, otherwise updates the
-- existing value. Sibling of SobekCM_Set_Setting_Value, same upsert shape.
CREATE PROCEDURE [dbo].[SobekCM_Set_Extension_Setting_Value]
	@Extension_Code nvarchar(50),
	@Setting_Key varchar(255),
	@Setting_Value varchar(max)
AS
BEGIN

	if ( ( select COUNT(*) from SobekCM_Settings where Extension_Code = @Extension_Code and Setting_Key = @Setting_Key ) > 0 )
	begin
		update SobekCM_Settings set Setting_Value = @Setting_Value where Extension_Code = @Extension_Code and Setting_Key = @Setting_Key;
	end
	else
	begin
		insert into SobekCM_Settings ( Setting_Key, Setting_Value, Hidden, Reserved, Extension_Code )
		values ( @Setting_Key, @Setting_Value, 1, 0, @Extension_Code );
	end;

END;
GO



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

GO

