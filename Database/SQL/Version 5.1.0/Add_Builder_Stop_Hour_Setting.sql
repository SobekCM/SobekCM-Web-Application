-- Adds the "Builder Stop Hour" setting, which makes the previously-hardcoded 11pm cutoff
-- (Worker_Controller.BULK_LOADER_END_HOUR) configurable. Value is the local hour (1-23) after
-- which the builder stops polling for new work and exits; 0 means never stop (poll indefinitely
-- until stopped externally, e.g. the machine itself being powered off - useful for installations
-- that start the builder at machine boot rather than on a fixed daily schedule).
--
-- Default of 23 preserves the previous hardcoded behavior for existing installations.

IF NOT EXISTS (SELECT 1 FROM [dbo].[SobekCM_Settings] WHERE [Setting_Key] = N'Builder Stop Hour')
BEGIN
	INSERT [dbo].[SobekCM_Settings] ([Setting_Key], [Setting_Value], [TabPage], [Heading], [Hidden], [Reserved], [Help], [Options], [Dimensions])
	VALUES (N'Builder Stop Hour', N'23', N'Builder', N'Builder Settings', 0, 0, N'Local hour (1-23) after which the builder stops polling for new work and exits. Use 0 to never stop (poll indefinitely until stopped externally).', N'0|20|21|22|23', NULL)
END
