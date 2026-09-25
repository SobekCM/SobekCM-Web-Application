@search
Feature: Narrowing search results
  Visitors narrow a search by clicking facet values in the "Narrow results by" column,
  by adding fields in the URL, or by limiting to a range of years.

  Scenario: The facet column lists values with their result counts
    Given I open "/results/?t=a"
    Then the "facet column" should contain "NARROW RESULTS BY:"
    And the facet "Subject: Spatial Coverage" should offer "Florida" with 13 results
    And the facet "Subject: Topics" should offer "Maps" with 12 results

  Scenario: Clicking a facet value narrows the search to that value
    Given I open "/results/?t=a"
    When I narrow the results by "Subject: Spatial Coverage" "Florida"
    Then the search explanation should contain "'Florida' in spatial coverage"
    And the search should report 13 matching records

  Scenario: A facet shows its top ten values until the visitor asks for more
    Given I open "/results/?t=a"
    Then the facet "Creator" should list 10 values
    When I click "Show More" in the "Creator" facet
    Then the facet "Creator" should list more than 10 values
    When I click "Show Less" in the "Creator" facet
    Then the facet "Creator" should list 10 values

  Scenario: Fields and operators in the URL are explained in plain words
    Given I open "/results/?t=map,sanborn&f=ZZ,-AU"
    Then the search explanation should read "Your search of All Collection Groups for 'map' anywhere and 'sanborn' not in creator resulted in 14 matching records."

  Scenario: A year range is explained, and a reversed range is put in order
    Given I open "/results/?t=map&yr1=1900&yr2=1800"
    Then the search explanation should contain "between 1800 and 1900"

  # BUG: facet values with accented characters are written as HTML-entity-like "#232;"
  # text, and the "#" starts a URL fragment, so the facet link searches for a cut-off value.
  @known-bug @fail
  Scenario: Clicking a facet value with accented characters finds its results
    Given I open "/results/?t=a"
    When I narrow the results by "Creator" "Andriveau-Goujon, E. (Eugène), 1832-1897"
    Then the search should report 2 matching records

  # BUG: the year range is echoed in the explanation, but the results aren't filtered by it:
  # 2020-2021 returns the same 19 records as no range at all.
  @known-bug @fail
  Scenario: A year range limits the results to those years
    Given I open "/results/?t=map&yr1=2020&yr2=2021"
    Then the search should report no matching records

  # BUG: da1/da2 are parsed from the URL but neither echoed nor applied, and the links the
  # page generates write dt1/dt2 instead.
  @known-bug @fail
  Scenario: An exact date range is applied to the search
    Given I open "/results/?t=map&da1=2020-01-01&da2=2021-01-01"
    Then the search should report no matching records
