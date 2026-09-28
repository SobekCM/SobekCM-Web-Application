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
