






  -- Add web content id to each of the web content statistics rows
  update SobekCM_WebContent_Statistics 
  set WebContentID = ( select WebContentID 
                       from SobekCM_WebContent W
                       where coalesce(W.Level1,'') = coalesce(SobekCM_WebContent_Statistics.Level1, '' )
                         and coalesce(W.Level2,'') = coalesce(SobekCM_WebContent_Statistics.Level2, '' )
                         and coalesce(W.Level3,'') = coalesce(SobekCM_WebContent_Statistics.Level3, '' )
                         and coalesce(W.Level4,'') = coalesce(SobekCM_WebContent_Statistics.Level4, '' )
                         and coalesce(W.Level5,'') = coalesce(SobekCM_WebContent_Statistics.Level5, '' )
                         and coalesce(W.Level6,'') = coalesce(SobekCM_WebContent_Statistics.Level6, '' )
                         and coalesce(W.Level7,'') = coalesce(SobekCM_WebContent_Statistics.Level7, '' )
                         and coalesce(W.Level8,'') = coalesce(SobekCM_WebContent_Statistics.Level8, '' ))
  where exists ( select WebContentID 
                       from SobekCM_WebContent W
                       where coalesce(W.Level1,'') = coalesce(SobekCM_WebContent_Statistics.Level1, '' )
                         and coalesce(W.Level2,'') = coalesce(SobekCM_WebContent_Statistics.Level2, '' )
                         and coalesce(W.Level3,'') = coalesce(SobekCM_WebContent_Statistics.Level3, '' )
                         and coalesce(W.Level4,'') = coalesce(SobekCM_WebContent_Statistics.Level4, '' )
                         and coalesce(W.Level5,'') = coalesce(SobekCM_WebContent_Statistics.Level5, '' )
                         and coalesce(W.Level6,'') = coalesce(SobekCM_WebContent_Statistics.Level6, '' )
                         and coalesce(W.Level7,'') = coalesce(SobekCM_WebContent_Statistics.Level7, '' )
                         and coalesce(W.Level8,'') = coalesce(SobekCM_WebContent_Statistics.Level8, '' ));
GO
   
  -- Delete any that are not linked                  
  delete from SobekCM_WebContent_Statistics
  where WebContentID is null;
GO
                     
					 
-- Insert statistics for a top-level web content page
ALTER PROCEDURE [dbo].[SobekCM_Statistics_Save_Webcontent]
	@year smallint,
	@month smallint,
	@hits int,
	@hits_complete int,
	@level1 varchar(100),
	@level2 varchar(100),
	@level3 varchar(100),
	@level4 varchar(100),
	@level5 varchar(100),
	@level6 varchar(100),
	@level7 varchar(100),
	@level8 varchar(100)
as
begin

	-- Get the WebContent ID
	declare @webcontentid int;
	set @webcontentid = coalesce((    select WebContentID 
									  from SobekCM_WebContent W
									  where coalesce(W.Level1,'') = coalesce(@level1, '' )
										 and coalesce(W.Level2,'') = coalesce(@level2, '' )
										 and coalesce(W.Level3,'') = coalesce(@level3, '' )
										 and coalesce(W.Level4,'') = coalesce(@level4, '' )
										 and coalesce(W.Level5,'') = coalesce(@level5, '' )
										 and coalesce(W.Level6,'') = coalesce(@level6, '' )
										 and coalesce(W.Level7,'') = coalesce(@level7, '' )
										 and coalesce(W.Level8,'') = coalesce(@level8, '' )), -1 );

	-- Only add if there is a web content id
	if ( @webcontentid > 0 )
	begin
		insert into SobekCM_Webcontent_Statistics ( WebContentID, Level1, Level2, Level3, Level4, Level5, Level6, Level7, Level8, [Year], [Month], [Hits], Hits_Complete ) 
		values ( @webcontentid, @level1, @level2, @level3, @level4, @level5, @level6, @level7, @level8, @year, @month, @hits, @hits_complete );
	end;

end;
GO