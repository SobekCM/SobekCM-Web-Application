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

  # Fixed 2026-09-26: with no other matches to suggest, the tokens inside the hidden block
  # ([%WithinInstanceUrl%] and the like) were left raw in the page and its link hrefs
  Scenario: The no-results page has no unreplaced template tokens
    Given I open "/results/?t=zzqqxxnomatch"
    Then the page should not contain unreplaced template tokens

  # Fixed 2026-09-26: the built-in no-results text was English only, and cached once for everyone
  @i18n
  Scenario: The no-results message is shown in the visitor's language
    Given I open "/results/?t=zzqqxxnomatch&lo=de"
    Then the search explanation should contain "ergab keine Treffer."
    And the "no results message" should not contain "Your search returned no results."
