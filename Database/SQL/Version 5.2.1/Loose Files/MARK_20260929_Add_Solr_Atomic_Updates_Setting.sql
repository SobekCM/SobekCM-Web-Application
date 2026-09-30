-- Adds the "Solr Atomic Updates Enabled" setting (off by default).  When on, the web application updates an
-- item's metadata in the Solr document index with an atomic update after a change that cannot affect its text
-- (such as editing its behaviors), rather than re-reading and resending all of its full text.  Solr rebuilds an
-- atomically updated document from its stored values, so turn this on only after the document index uses the
-- current schema.xml, which now stores every field, and has been fully reindexed.  With it off, the whole item
-- is reindexed instead.
--
-- TODO v6.0: remove this setting (and always use atomic updates), since v6.0 needs a full reindex anyway.

if ( not exists ( select 1 from dbo.SobekCM_Settings where Setting_Key = 'Solr Atomic Updates Enabled' ))
begin
	insert into dbo.SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options )
	values ( 'Solr Atomic Updates Enabled', 'false', 'System / Server Settings', 'Search Preferences', 0, 2, 'Flag indicates whether the Solr document index can use atomic updates.  When on, a change that cannot affect an item''s text (such as editing its behaviors) updates just that item''s metadata in the index, without re-reading its full text.  Turn this on only once the document index uses the current schema.xml (which stores every field) and has been fully reindexed.', 'true|false' );
end;
GO
