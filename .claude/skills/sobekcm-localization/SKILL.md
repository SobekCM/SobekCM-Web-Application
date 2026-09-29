---
name: sobekcm-localization
description: How SobekCM's interface translations work - where the files live (English in config\default, every other language in its own plugin), the order they load and override each other, the general vs. keyed categories, and the conventions for adding or fixing a translation. Use before adding, changing or debugging any translated text, or when a translation works in the repository but not on a site.
---

# SobekCM localization

## What must be localized

In priority order:

1. **The public interface: always.** Everything an anonymous or ordinary visitor can see: home and collection pages, search forms and results, item pages and viewers, the header, footer and menus, and so on. New public text is never hard-coded English.
2. **Basic item management: generally supported.** What an item's submitter or editor uses: uploading an item, editing its metadata, adding spatial data, managing its files, and the related mySobek screens.
3. **Collection management: lowest priority, not done today.** Don't treat missing translations here as a bug. Localize new collection-management text only when asked.

**Purely administrative screens are excluded for now:** anything only a system admin or host admin can do, and screens a portal admin can view but not change. Leave their text as English. Don't flag it as a localization gap, and don't add translation keys for it unasked. Some admin screens were localized earlier; keep those translations, since this rule is about new work, not removing what exists.

## Where the files live

| What | Location |
|---|---|
| English (the required, complete default) | `Code/SobekCM/config/default/localization/en/sobekcm_localization_<category>_en.config` |
| Every other language | `Code/SobekCM/plugins/<Language>/config/localization/<code>/sobekcm_localization_<category>_<code>.config` |
| Language plugin definition | `Code/SobekCM/plugins/<Language>/Plugin.config` (its `<Languages>` element names the code) |
| Translated HTML fragments (quick tips, map FAQs, ...) | `Code/SobekCM/plugins/<Language>/design/extra/aggregations/<name>_<CODE>.html` |
| Which languages a site offers | `Code/SobekCM/config/default/sobekcm_language_support.config` |

The language plugins are `Dutch` (nl), `French` (fr), `German` (de), `Italian` (it), `Portugese` (pt, spelled that way in the folder name) and `Spanish` (es). Only English stays under `config\default\localization`. **Never add a non-English file under `config\default\localization`.** Non-English translations used to live there too, and a fix made in one copy was silently hidden by the other (see "Why a fix doesn't show up" below).

The English HTML fragments live in the site's `design\extra\aggregations`, which is git-ignored, so they aren't in the repository.

## Categories

Each language has six files, one per category: `aggregations`, `chrome`, `general`, `internal`, `items` and `mysobek`. There are two kinds:

- **`general` is keyed by the literal English text.** `Localization_Gateway.General.Get(term, language)` (and the older `UI_ApplicationCache_Gateway.Translation.Get_Translation(...)`, which just calls it) looks up the exact string the code passes in, from the single `General` section. Used for field labels, facet and metadata values, collection names, aggregation type labels ("Subcollections", "Collections", ...) and so on.
  - The key must be **exactly** the text the caller passes, including spaces. The lookup ignores case, but underscores don't match spaces: `Key="Alternate_Title"` never matches `"Alternate Title"`. This bug once broke every multi-word term.
  - Non-English `general` files are **sparse**: they only list terms whose translation differs from the English. Differing key sets across languages are expected. Don't add a phrase whose value equals its key.
  - A term with no entry is shown unchanged in English, with no marker, so a missing translation is easy to miss.
- **Every other category uses fixed phrase keys** via `Localization_Gateway.<Section>.<Method>(language)`, for example `Localization_Gateway.Common.Next_Page(language)` → `Localization_Store.Get("items", "Common", "Next_Page", language)`. The nested class name is the `<Section Name>` in the file, and the method name is the `Phrase Key`.
  - Every key must exist in English. A missing key falls back to English, and if English is missing it too, the page shows `[[Section.Key]]`.
  - Section names must be unique within a category, since they share one file per language.

## Load order: later folders win, key by key

`Configuration_Files_Reader` builds the list of folders, and `Localization_Store.Read_File` reads the same file name from every folder in this order, merging key by key (a later value replaces an earlier one; keys a file doesn't list are left alone):

1. `config\default\localization`
2. each **enabled** plugin's `localization`, `config\localization` or `config\default\localization` folder, in alphabetical order of plugin folder
3. `config\user\localization` (a site's own additions, such as its collection names; survives upgrades)

A language plugin's files are only read if that extension is enabled for the site (`InstanceWide_Settings.ExtensionEnabled`, from the database). The files load lazily, the first time a language/category pair is needed, and are cached. `Localization_Gateway.Clear_Cache()` (part of `UI_ApplicationCache_Gateway.ResetAll()`) picks up edited files without a restart.

## Adding or changing a translation

1. **New text in code:** don't hard-code it. Either pass the English text through `General.Get` (content-like terms), or add a keyed phrase: an English `Phrase` in the right category's `en` file, a method on the matching `Localization_Gateway` nested class, and a call site that passes `RequestSpecificValues.Current_Mode.Language`. Language is always passed explicitly; don't use ambient or thread-local state.
2. **Translate it** in each language plugin's file for the same category. Keep the existing wording for related terms (for example, Italian uses "Raccolte" for collections, so a new term about collections should too).
3. **Match the files' existing encoding and line endings.** They're UTF-8 with CRLF. Git Bash's `sed -i` silently converts CRLF to LF, so check `git diff --stat` before finishing.
4. **Check the XML is still valid.** A parse error loses the whole file for that language.

## Why a fix doesn't show up

- **Only one language is wrong, and the repository file is right:** another copy of that file loaded later is overriding it. On a site, check the language plugin folder (`plugins\<Language>\config\localization\<code>\`) and `config\user\localization\<code>\`. The e2e test environment restores the testing site's customer folders, so an old copy on the testing site breaks the e2e run too.
- **A multi-word `general` term never translates:** the key doesn't exactly match the text the caller passes (underscores vs. spaces, or different wording). Find the call site and compare.
- **A label built from data** (for example, aggregation child types become "<Type>s" in `Engine_Database.add_children`): the key is the computed string, so there has to be a phrase for each possible value.

## Choosing the language for a request

The repository's `CLAUDE.md` ("UI language selection") covers how the language is picked (`lo=`, `l=`, session, user preference, browser) and why `l=` must not be added back into generated URLs.
