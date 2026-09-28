@item
Feature: Item citation, metadata and breadcrumbs
  Every digital item has a citation ("Standard View") page as its default landing view, plus
  MARC and structured-metadata alternates, reachable from the item's own menu. The item's
  breadcrumb trail names every collection it belongs to.

  # This item defaults to its page-image viewer at the bare item URL (it has real pages), so the
  # citation view needs its own explicit URL - unlike the dark/private/empty items elsewhere in
  # this suite, which have no pages and so land on citation by default.
  Scenario: An item's citation page shows its title and a working thumbnail
    Given I open "/AA00001559/00001/citation"
    Then the response status should be 200
    And the page title should contain "Beginning Algebra Part I Workbook"
    And every image in the "citation thumbnail" should load

  Scenario: The item menu offers print, and send asks anonymous visitors to log on
    Given I open "/AA00001559/00001"
    Then the "item print button" should be visible
    When I click the "item send button" region
    Then the URL should contain "/my/logon"

  Scenario: MARC and Metadata views render real content
    Given I open "/AA00001559/00001/marc"
    Then the response status should be 200
    And the "marc view" should be visible

  Scenario: The Metadata view renders real content
    Given I open "/AA00001559/00001/metadata"
    Then the response status should be 200
    And the "metadata viewer" should be visible

  Scenario: An unknown BibID shows the Page Not Found page
    Given I open "/ZZ99999999/00001"
    Then the response status should be 404
    And the "page not found panel" should contain "Page Not Found"

  Scenario: An unknown VID for a real item falls back to a real volume
    Given I open "/AA00001559/99999"
    Then the response status should be 200
    And I should be on "/AA00001559/00001"

  # dr00000037/00001 is in HIDDEN-MAPS (should show) and INACTIVE-MAPS (should not) - closes the
  # item-breadcrumb question the aggregation-phase plan deferred (see the BDD plan's TODO section)
  Scenario: An item's breadcrumb trail includes hidden collections but not inactive ones
    Given I open "/DR00000037/00001"
    Then the breadcrumbs should read "Testing Home | Maps Collection | Historic Maps | Hidden Maps Collection | David Rumsey Map Collection"
