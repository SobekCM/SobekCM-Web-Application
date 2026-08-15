-- Adds the "Builder Stop Hour" setting, which makes the previously-hardcoded 11pm cutoff
-- (Worker_Controller.BULK_LOADER_END_HOUR) configurable. Value is the local hour (1-23) after
-- which the builder stops polling for new work and exits; 0 means never stop (poll indefinitely
-- until stopped externally, e.g. the machine itself being powered off - useful for installations
-- that start the builder at machine boot rather than on a fixed daily schedule).
--
-- Default of 23 preserves the previous hardcoded behavior for existing installations.

insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, Dimensions)
values ('Builder Stop Hour', '23', 'Builder', 'Builder Settings', false, 0, 'Local hour (1-23) after which the builder stops polling for new work and exits. Use 0 to never stop (poll indefinitely until stopped externally).', '0|20|21|22|23', NULL)
on conflict (Setting_Key) do nothing;
