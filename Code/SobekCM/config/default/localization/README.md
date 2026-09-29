# Localization files

Only the **English** files live here (`en/`). English is the required, complete default.

Every other language is its own plugin, under `Code/SobekCM/plugins/<Language>/config/localization/<code>/`. Don't add a non-English file here: plugin folders load after this one, so a copy here would be silently overridden.

See the `sobekcm-localization` skill (`.claude/skills/sobekcm-localization/SKILL.md` at the repository root) for how these files load, the difference between the `general` and keyed categories, and how to add or fix a translation.
