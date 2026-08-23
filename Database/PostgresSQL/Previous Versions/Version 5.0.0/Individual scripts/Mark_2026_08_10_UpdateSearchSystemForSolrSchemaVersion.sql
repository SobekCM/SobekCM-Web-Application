	update SobekCM_Settings set Setting_Value='Solr 7', Options='Solr 7|Solr 9+', Help='Which system and schema to use for searching - "Solr 7" uses the legacy Solr field names (safe for Solr 7 and older); "Solr 9+" uses the updated docValues-backed sort/group field names, which requires the current schema.xml and a full reindex.' where Setting_Key='Search System';

