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

  # dr00000037/00001 is in HIDDEN-MAPS (should show), INACTIVE-MAPS (should not) and
  # IRUMSEY-MAPS/"David Rumsey Map Collection" (the item's institution). Closes the item-breadcrumb
  # question the aggregation-phase plan deferred.
  #
  # HeaderFooter_HtmlHelper.Add_Header filters a plain related collection by Active only, but
  # filters the item's Source_Institution_Aggregation/Holding_Location_Aggregation by
  # "!Hidden && Active" - stricter for the institution role. Confirmed intentional, not a bug: an
  # institution (created through the Add Collection Wizard) shouldn't ever be Hidden in practice.
  # This scenario only exercises the plain-related-collection path (Hidden Maps Collection is
  # neither this item's source nor holding institution) - see the BDD plan for the still-open
  # question of confirming the wizard actually prevents a Hidden institution.
  Scenario: An item's breadcrumb trail includes hidden collections but not inactive ones
    Given I open "/DR00000037/00001"
    Then the breadcrumbs should read "Testing Home | Maps Collection | Historic Maps | Hidden Maps Collection | David Rumsey Map Collection"

  # The "Egypt" subject term links to a quoted, exact-match, "All Subjects" (SU) search - opens in
  # a new tab (target="_BLANK"), so this checks the link's own href rather than navigating.
  Scenario: A linked subject term in the citation points at a correctly-formatted search
    Given I open "/DR00000037/00001/citation"
    Then the "Egypt" link should have an href containing "/contains/"
    And the "Egypt" link should have an href containing "f=SU"
