@item
Feature: An item's text search after a full-text search
  A full-text search result links into the item's own text search, already run for the same
  words, so the visitor sees which pages matched.

  # Known bug, 2026-09-29: the results page decides from Solr that this item has text (it shows a
  # snippet), but the item's TextSearchable database flag is off, so the item doesn't offer its
  # "Search" view and the link falls back to the page images. The flag is set by the Builder's
  # SaveToDatabaseModule, which until recently missed page text stored only in GCS. Remove the tags
  # once the item is reprocessed (or the results page stops linking to a search view the item lacks).
  @known-bug @fail
  Scenario: A full-text result opens the item's text search, with the matching pages listed
    Given the item "UF00076840/00001" is full-text searchable
    And I open "/juvenile/results/?text=caterpillar"
    When I click the "Alice's adventures in wonderland" link in the "brief results"
    Then I should be on "/UF00076840/00001/search?search=caterpillar"
    And the selected item tab should be "Search"
    And the "item search box" should be present
    And the "item text search results" should contain "caterpillar"
