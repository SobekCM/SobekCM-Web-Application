@item
Feature: Format-specific item viewers
  Items with a PDF or downloadable files get a matching tab; items with real-world
  coordinates or full-text get a Map and Search tab as well.

  Scenario: An item with a PDF shows a download link and an embedded preview
    Given I open "/AA00001559/00001/pdf"
    Then the response status should be 200
    And the "pdf viewer" should be visible

  Scenario: The downloads tab lists every downloadable file
    Given I open "/AA00001559/00001/downloads"
    Then the "downloads viewer" should contain "This item has the following downloads:"
    And I should see "( .pdf )"
    And I should see "( .docx )"

  Scenario: A geo-located item's Map It! tab loads
    Given I open "/UF00071726/00003/map"
    Then the response status should be 200

  # Text_Search_ItemViewer - full-text search within one item's own pages, distinct from the
  # site-wide/aggregation search boxes tested elsewhere. Its quick tips repeat the same
  # phrase-searching convention ("natural history") documented for the main search boxes.
  Scenario: A newspaper's within-item full-text search shows its own quick tips
    Given I open "/NDNP000003/00001/search"
    Then the "item search box" should be visible
    And I should see "Phrase Searching"
    And I should see "natural history"

  # Confirms the Search tab is really driven by the engine's own textSearchable flag and the
  # presence of real text files - not just "this happens to be a newspaper" - by checking both of
  # those against the engine's item data before checking the rendered menu.
  Scenario: An item flagged full-text searchable with real text files shows the Search tab
    Given the item "NDNP000003/00001" is full-text searchable
    And the item "NDNP000003/00001" has a "txt" file
    When I open "/NDNP000003/00001"
    Then the "item menu" should contain "Search"
