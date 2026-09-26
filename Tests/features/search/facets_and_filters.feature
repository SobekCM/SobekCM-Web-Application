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

  # Fixed 2026-09-26: accented values used to be written as "#232;" text, and the "#" started
  # a URL fragment, so the facet link searched for a cut-off value
  Scenario: Clicking a facet value with accented characters finds its results
    Given I open "/results/?t=a"
    When I narrow the results by "Creator" "Andriveau-Goujon, E. (Eugène), 1832-1897"
    Then the search should report 2 matching records

  # Fixed 2026-09-26: the range was echoed but never passed on to Solr, so 2020-2021 returned
  # the same 19 records as no range at all
  Scenario: A year range limits the results to those years
    Given I open "/results/?t=map&yr1=2020&yr2=2021"
    Then the search should report no matching records

  # Fixed 2026-09-26: da1/da2 were parsed but neither echoed nor applied, and the page's own
  # links wrote them as dt1/dt2 (with the start date twice), losing the range when paging
  Scenario: An exact date range is applied to the search
    Given I open "/results/?t=map&da1=2020-01-01&da2=2021-01-01"
    Then the search should report no matching records
