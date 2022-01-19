USE [sobektest]
GO

/****** Object:  StoredProcedure [dbo].[mySobek_Save_DefaultMetadata]    Script Date: 1/19/2022 6:01:36 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


-- Add a new default metadata set to this database
CREATE PROCEDURE [dbo].[mySobek_Save_DefaultMetadata]
	@metadata_code varchar(20),
	@metadata_name varchar(100),
	@description varchar(255),
	@userid int
AS
BEGIN
	
	-- Does this project already exist?
	if (( select count(*) from mySobek_DefaultMetadata where MetadataCode=@metadata_code ) > 0 )
	begin
		-- Update the existing default metadata
		update mySobek_DefaultMetadata
		set [Description] = @description, [MetadataName] = @metadata_name
		where MetadataCode = @metadata_code;
	end
	else
	begin
		-- Add a new set
		insert into mySobek_DefaultMetadata ( [Description], MetadataCode, UserID, MetadataName )
		values ( @description, @metadata_code, @userid, @metadata_name );
	end;
END;
GO

