@item
Feature: An item's text search after a full-text search
  A full-text search result links into the item's own text search, already run for the same
  words, so the visitor sees which pages matched.


  # UF00076840 ("Alice's adventures in wonderland") has page text for every page, so the search finds
  # many pages. Until it was reprocessed on 2026-09-29 its TextSearchable flag was a stale 0, so the
  # item didn't offer its Search view and this link fell back to the page images. The page count is
  # the testing site's data; update it if the item's text changes.
  Scenario: A full-text result opens the item's text search, with the matching pages listed
    Given the item "UF00076840/00001" is full-text searchable
    And I open "/juvenile/results/?text=caterpillar"
    When I click the "Alice's adventures in wonderland" link in the "brief results"
    Then I should be on "/UF00076840/00001/search?search=caterpillar"
    And the selected item tab should be "Search"
    And the "item search box" should be present
    And I should see "resulted in ten matching pages"
    And every page in the item's search results should highlight "caterpillar"
