@search
Feature: Search results page
  A search shows how many records matched, twenty results per page, and lets the
  visitor page through, re-sort, and switch between brief, table and thumbnail views.

  Background:
    Given I open "/results/?t=a"

  Scenario: The results page explains the search and how many records matched
    Then the response status should be 200
    And the page title should be "Testing Search Results - All Collection Groups"
    And the search explanation should read "Your search of All Collection Groups for 'a' anywhere resulted in {total} matching records."
    And the result range should read "1 - 20 of {total} matching titles"
    And the page should list 20 results
    And every result should link to an item page
    And the page should have no script errors

  Scenario: The first page offers only forward paging
    Then there should be a "next" page button
    And there should be a "last" page button
    And there should be no "previous" page button
    And there should be no "first" page button

  Scenario: Moving to the next page shows the next results
    When I go to the "next" results page
    Then I should be on "/results/brief/2/?t=a"
    And the result range should cover results page 2
    And the page should list one result for every title in the range
    And there should be a "previous" page button
    And there should be a "first" page button

  Scenario: The last page shows the remaining results and offers no forward paging
    When I go to the "last" results page
    Then the result range should cover the last results page
    And the page should list one result for every title in the range
    And there should be a "previous" page button
    And there should be a "first" page button
    And there should be no "next" page button

  Scenario: The first page button returns to the first page
    When I go to the "last" results page
    And I go to the "first" results page
    Then the result range should read "1 - 20 of {total} matching titles"

  Scenario: A search with twenty or fewer results has no paging buttons
    Given I open "/juvenile/results/?t=a"
    Then the search should report 5 matching records
    And there should be no paging buttons

  Scenario: Anonymous visitors can sort by rank, title and date
    Then the sort options should be "Rank, Title, Date Ascending, Date Descending"

  Scenario: Sorting by title reorders the results alphabetically
    When I sort the results by "Title"
    Then the URL should contain "o=1"
    And the result titles should be in alphabetical order

  Scenario Outline: Results can be shown in different views
    When I click the "<view> View" link in the "main menu"
    Then the URL should contain "/results/<code>/"
    And the results should be shown in the "<name>" view
    And the page should list 20 results

    Examples: <view>
      | view      | code   | name      |
      | Brief     | brief  | brief     |
      | Table     | table  | table     |
      | Thumbnail | thumbs | thumbnail |

  Scenario: The chosen view is kept when paging
    When I click the "Table View" link in the "main menu"
    And I go to the "next" results page
    Then I should be on "/results/table/2/?t=a"
    And the results should be shown in the "table" view

  Scenario: An unknown view code falls back to the brief view
    Given I open "/results/bogus/?t=a"
    Then the "brief results" should be visible

  Scenario: Removing the only search term browses all items
    When I remove the search term "a"
    Then the URL should contain "/all"

  Scenario: Print is offered, and saving or emailing a search asks anonymous visitors to log on
    Then the "print button" should be visible
    When I click the "send button" region
    Then the URL should contain "/my/logon"

  Scenario Outline: Searches can ask for exact wording or similar meaning
    Given I open "/<precision>/?t=map"
    Then the response status should be 200
    And the search should report some matching records

    Examples: <precision>
      | precision   |
      | contains    |
      | exact       |
      | resultslike |

  Scenario: A full-text search finds words inside the items' text and highlights them
    Given I open "/results/?text=map"
    Then the search explanation should contain "'map' in full text"
    And the search should report 2 matching records
    And the "highlighted search text" should be visible

  # Fixed 2026-09-26: the button's id was "sharebutton" while toggle_share_form2 looks up
  # #share_button (TypeError), and it was passed none of the title/URL/icon values the form needs
  Scenario: The share button opens the share links
    When I click the "share button" region without leaving the page
    Then the "share form" should be visible
    And the page should have no script errors

  # Fixed 2026-09-26: the bottom bar printed Showing_Text, which was never set
  Scenario: The bottom paging bar repeats the result range
    Then the bottom paging bar should show the result range

  # Fixed 2026-09-26: the Solr results mapper never set the items' publication date, so the
  # Date column was always empty
  Scenario: The table view shows each result's date
    Given I open "/results/table/?t=palestine"
    Then the "results table" should contain "1876"

  # Fixed 2026-09-26: UrlInitializer overwrote the pre-rewrite original URL with the rewritten
  # one, so share links and every itemNavForm action carried ?urlrelative=...
  Scenario: Links on the results page never expose the internal urlrelative parameter
    Then the page HTML should not contain "urlrelative"
