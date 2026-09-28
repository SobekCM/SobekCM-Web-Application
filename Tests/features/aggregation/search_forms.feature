@aggregation @search
Feature: Searching from a collection's search forms
  Each collection offers a basic search box on its home page, plus the advanced and
  full-text search forms from its main menu. Searching from a collection stays
  inside that collection.

  Scenario: The basic search box is ready to type into
    Given I open the "maps" collection
    Then the "search prompt" should contain "Search Collection:"
    And the "search box" should have focus

  Scenario: Searching from a collection home page searches only that collection
    Given I open the "maps" collection
    When I search for "egypt"
    Then I should be on "/maps/results/?t=egypt"
    And the page title should be "Testing Search Results - Maps Collection"
    And the search explanation should contain "Your search of Maps Collection for 'egypt' anywhere"
    And the search should report 4 matching records

  Scenario: Pressing Enter in the search box runs the search
    Given I open the "maps" collection
    When I search for "egypt" by pressing Enter
    Then I should be on "/maps/results/?t=egypt"

  Scenario: Searching with an empty box browses all of the collection's items
    Given I open the "maps" collection
    When I search for ""
    Then I should be on "/maps/all"

  Scenario: Searching from the top-level home page searches the whole site
    Given I open "/"
    When I search for "egypt"
    Then I should be on "/results/?t=egypt"
    And the search should report 14 matching records

  Scenario: The advanced search form offers four search rows
    Given I open "/maps/advanced"
    Then the page title should be "Testing Search - Maps Collection"
    And the "advanced search panel" should be visible
    And the "Advanced Search" main menu item should be selected

  Scenario: Advanced search combines terms with fields and operators
    Given I open "/maps/advanced"
    When I run an advanced search for:
      | operator | term    | field    |
      |          | map     | Anywhere |
      | and not  | sanborn | Creator  |
    Then the URL should contain "/maps/results/"
    And the search explanation should contain "'map' anywhere and 'sanborn' not in creator"
    And the search should report some matching records

  # Known, verified data (via the resource DB, 2026-09-28): within "maps", Creator "Sanborn Map
  # Company" is 5 items/1 title, Creator "U.S. Department of Agriculture" is 2 items/1 title, and
  # nothing else in the collection has either creator - so "or" between them has to total exactly 7,
  # a real union, not a coincidence of overlapping data.
  Scenario: Advanced search "or" broadens the results instead of narrowing them
    Given I open "/maps/advanced"
    When I run an advanced search for:
      | operator | term                           | field   |
      |          | Sanborn Map Company            | Creator |
      | or       | U.S. Department of Agriculture | Creator |
    Then the URL should contain "/maps/results/"
    And the search should report 7 matching records

  # Same two creators, plus a third clause that excludes one side of the "or" by a term unique to
  # its title ("1937" appears only in the U.S. Department of Agriculture item's title, not
  # Sanborn's) - a single query exercising and/or/and-not together, not just two of the three.
  Scenario: Advanced search combines "or" and "and not" in the same query
    Given I open "/maps/advanced"
    When I run an advanced search for:
      | operator | term                            | field    |
      |          | Sanborn Map Company             | Creator  |
      | or       | U.S. Department of Agriculture  | Creator  |
      | and not  | 1937                             | Anywhere |
    Then the URL should contain "/maps/results/"
    And the search should report 5 matching records

  Scenario Outline: The advanced search precision choice picks the search type
    Given I open "/advanced"
    When I type "map" into advanced search row 1
    And I choose the "<precision>" search precision
    And I run the advanced search
    Then the URL should contain "<path>"

    Examples: <precision>
      | precision                                          | path           |
      | Contains exactly the search terms                  | /contains/     |
      | Contains any form of the search terms              | /results/      |
      | Contains the search term or terms of similar meaning | /resultslike/ |

  Scenario: The text search form searches the full text of items
    Given I open "/maps/text"
    Then the page title should be "Testing Search - Maps Collection"
    And the "full text search panel" should contain "Search full text:"
    When I search for "map"
    Then the URL should contain "text=map"

  Scenario: The browse-all view lists the collection's items with paging and sorting
    Given I open "/maps/all"
    Then the response status should be 200
    And the page title should be "Testing - Maps Collection"
    And the "results table" should be visible
    And the "sort dropdown" should be visible
    And every result should link to an item page

  Scenario: A metadata browse-by field with no matching values says so
    Given I open "/browseby/etd_degree_grantor"
    Then the response status should be 200
    And I should see "NO MATCHING VALUES"

  Scenario: A browse-by field with many values paginates into letter groups
    Given I open "/browseby/subject_keyword"
    Then I should see "AB"
    When I click the "CDE" link
    Then I should be on "/browseby/subject_keyword/2"

  Scenario: Checking the full-text option searches the document text instead of metadata
    Given I open the "juvenile" collection
    When I check the full-text search option
    And I search for "pirate"
    Then the URL should contain "text=pirate"
    And the URL should not contain "t=pirate"

  Scenario: The newspaper search form builds a field-specific search
    Given I open "/newspapers/newspaper"
    When I choose "Newspaper Title" as the newspaper search field
    And I search for "Gaceta"
    Then the URL should contain "f=TI"

  # "Gaceta" is a newspaper title (see newspapers/all); the site-wide full-text search page has a
  # "#newscheck" / "Include newspapers?" checkbox that is unchecked by default, so newspaper text
  # should be excluded from the search by default. Confirm this reading of the checkbox before
  # relying on it elsewhere: an unchecked box could instead mean "don't ask about newspapers" with
  # no filtering effect.
  Scenario: Excluding newspapers from a full-text search leaves out newspaper content
    Given I open "/text"
    When I search for "Gaceta"
    Then the search should report no matching records
