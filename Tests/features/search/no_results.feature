@search
Feature: Searches that find nothing
  When a search matches no records, the visitor is told so and pointed at other places
  to look.

  Scenario: A search with no matches says so and suggests other resources
    Given I open "/results/?t=zzqqxxnomatch"
    Then the response status should be 200
    And the search should report no matching records
    And the "no results message" should contain "Your search returned no results."
    And I should see "Google Scholar"
    And I should see "Worldcat"
    And the "facet column" should not be present
    And there should be no paging buttons

  Scenario: Removing the search term from a search with no matches browses all items
    Given I open "/results/?t=zzqqxxnomatch"
    When I remove the search term "zzqqxxnomatch"
    Then I should be on "/all"

  # BUG: the no-results template leaves [%WithinInstanceCount%]-style tokens in the page
  # (inside a hidden div, but still in the delivered HTML and link hrefs).
  @known-bug @fail
  Scenario: The no-results page has no unreplaced template tokens
    Given I open "/results/?t=zzqqxxnomatch"
    Then the page should not contain unreplaced template tokens

  # BUG: the "no matching records" sentence is localized, but the "Your search returned no
  # results." message under it stays in English.
  @known-bug @fail @i18n
  Scenario: The no-results message is shown in the visitor's language
    Given I open "/results/?t=zzqqxxnomatch&lo=de"
    Then the search explanation should contain "ergab keine Treffer."
    And the "no results message" should not contain "Your search returned no results."
