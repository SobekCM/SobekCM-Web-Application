


CREATE PROCEDURE [dbo].mySobek_Add_User_Setting
	@userid int,
	@setting_key nvarchar(255),
	@setting_value nvarchar(max)
AS
begin

	-- Does this already exist?
	if ( (select count(*) from mySobek_User_Settings where UserID=@userid and Setting_Key=@setting_key ) > 0 )
	begin
		-- If this clears the value, remove the key
		if ( len(@setting_value) > 0 ) 
		begin
			delete from mySobek_User_Settings where UserID=@userid and Setting_Key=@setting_key;
		end
		else
		begin
			update mySobek_User_Settings 
			set Setting_Value=@setting_value
			where UserID=@userid and Setting_Key=@setting_key;
		end;
	end
	else
	begin

		insert into mySobek_User_Settings ( UserID, Setting_Key, Setting_Value )
		values ( @userid, @setting_key, @setting_value );

	end;
	
end;
GO


CREATE PROCEDURE [dbo].mySobek_Clear_User_Settings
	@userid int
AS 
begin

	delete from mySobek_User_Settings
	where UserID=@userid;

end;
GO

-- Add new system-wide settings
insert into SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options )
values ( 'Use Tracking Sheet', 'false', 'Digital Resource Settings', 'Online Management Settings', 0, 0, 'Whether the administrative options to use the tracking sheet will be displayed', 'true|false' );
GO

insert into SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options )
values ( 'Manage GeoSpatial Data', 'false', 'Digital Resource Settings', 'Online Management Settings', 0, 0, 'Whether the beta options to manage geo-spatial data will be displayed', 'true|false' );
GO

insert into SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options )
values ( 'Allow Mass Behavior Update', 'false', 'Digital Resource Settings', 'Online Management Settings', 0, 0, 'Whether the administrative options to mass update the behaviors is available', 'true|false' );
GO
