
CREATE TABLE [dbo].[SobekCM_OpenPublishing_Theme](
	[ThemeID] [int] IDENTITY(1,1) NOT NULL,
	[ThemeName] [nvarchar](150) NOT NULL,
	[Location] [varchar](255) NOT NULL,
	[Author] [nvarchar](50) NULL,
	[Description] [nvarchar](1000) NULL,
	[Image] [nvarchar](255) NULL,
	[AvailableForSelection] bit NOT NULL default('true')
 CONSTRAINT [PK_OpenPublishing_Theme] PRIMARY KEY CLUSTERED 
(
	[ThemeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO


CREATE PROCEDURE [dbo].[SobekCM_Get_OpenPublishing_Theme]
	@id int
AS
begin

	select ThemeID, ThemeName, Location, isnull(Author,'') as Author, isnull([Description],'') as [Description], isnull([Image], '') as [Image], AvailableForSelection
	from SobekCM_OpenPublishing_Theme
	where ThemeID=@id;

end;
GO


CREATE PROCEDURE [dbo].[SobekCM_Get_Available_OpenPublishing_Themes]
	@id int
AS
begin

	select ThemeID, ThemeName, Location, isnull(Author,'') as Author, isnull([Description],'') as [Description], isnull([Image], '') as [Image], AvailableForSelection
	from SobekCM_OpenPublishing_Theme
	where AvailableForSelection='true';

end;
GO

GRANT EXECUTE ON SobekCM_Get_OpenPublishing_Theme to sobek_user;
GRANT EXECUTE ON SobekCM_Get_Available_OpenPublishing_Themes to sobek_user;
GO


