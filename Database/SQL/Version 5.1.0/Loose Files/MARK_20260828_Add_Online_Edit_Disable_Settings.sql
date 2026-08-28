-- Re-adds 'Can Submit Edit Online' as a genuinely distinct flag from 'Can Submit Items Online':
-- the existing 'Can Submit Items Online' setting only ever gated creating a brand-new item/volume
-- (New_Item, New_TEI_Item, etc). This one gates ANY change to an EXISTING item -- metadata,
-- behaviors, permissions, deletion, and so on. The key existed in schema history back to v4 but
-- was dropped from the live schema at some point and was never wired into any C# code, so this
-- check guards against re-inserting a duplicate on a DB that still has the old row.
--
-- Also adds 'Disabled Online Changes Link': when a user reaches a mySobek viewer that would let
-- them submit a new item or edit an existing one while the relevant flag above is off, they're
-- now redirected here instead of just seeing an inline disabled message -- previously the flag
-- only hid the menu link, it never actually stopped someone who had a direct URL. Left blank,
-- the redirect falls back to the site's main home page instead of a configured link.

if not exists (select 1 from dbo.SobekCM_Settings where Setting_Key = 'Can Submit Edit Online')
begin
	insert into dbo.SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options )
	values ( 'Can Submit Edit Online', 'true', 'System / Server Settings', 'Disable Behavior', 0, 2, 'Flag dictates if users can make ANY changes to an existing item online (metadata, behaviors, permissions, deletion, etc) -- separate from submitting a brand-new item, which is controlled by "Can Submit Items Online".', 'true|false' );
end
else
begin
	update dbo.SobekCM_Settings set Reserved=2, TabPage='System / Server Settings', Heading='Disable Behavior' where Setting_Key='Can Submit Edit Online';
end;
	

if not exists (select 1 from dbo.SobekCM_Settings where Setting_Key = 'Disabled Online Changes Link')
begin
	insert into dbo.SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help )
	values ( 'Disabled Online Changes Link', '', 'System / Server Settings', 'Disable Behavior', 0, 2, 'When set, a user who reaches a mySobek viewer that would let them submit a new item or edit an existing one -- while online submissions/edits are disabled -- is redirected here instead of just being shown a disabled message. Leave blank to fall back to the site''s main home page.' );
end
else
begin
	update dbo.SobekCM_Settings set Reserved=2, TabPage='System / Server Settings', Heading='Disable Behavior' where Setting_Key='Disabled Online Changes Link';
end;

update dbo.SobekCM_Settings set Reserved=2, TabPage='System / Server Settings', Heading='Disable Behavior' where Setting_Key='Can Submit Items Online';
GO
