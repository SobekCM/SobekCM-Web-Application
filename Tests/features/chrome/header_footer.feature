@chrome
Feature: Site header and footer
  Every public page carries the web skin's header (branding, account link, breadcrumbs),
  the collection banner, and a footer with language links and the software version.

  Scenario: The home page header offers the account link and no breadcrumbs
    Given I open "/"
    Then the "header" should be visible
    And the account link should lead to the logon page
    And there should be no breadcrumbs

  Scenario Outline: Pages inside a collection show breadcrumbs back to it
    Given I open "<page>"
    Then the breadcrumbs should read "Testing Home | Maps Collection"

    Examples: <page>
      | page                   |
      | /maps/results/?t=egypt |
      | /maps/all              |
      | /maps/advanced         |

  Scenario: Clicking a breadcrumb returns to that collection
    Given I open "/maps/results/?t=egypt"
    When I click the "Maps Collection" link in the "breadcrumbs"
    Then I should be on "/maps"

  Scenario: The collection banner is shown and its image loads
    Given I open the "maps" collection
    Then the "banner" should be visible
    And every image in the "banner" should load

  Scenario: The footer offers every configured language, with the current one not linked
    Given I open "/"
    Then the footer should offer these languages: "Dutch, English, French, German, Spanish"
    And the current language "English" should not be a link

  Scenario: The footer shows the software version and the current year
    Given I open "/"
    Then the footer should show the expected software version
    And the footer should show the current year

  Scenario Outline: The skip link points at the page's main content
    Given I open "<page>"
    Then the skip link should point at the main content

    Examples: <page>
      | page          |
      | /             |
      | /maps         |
      | /results/?t=a |

  Scenario Outline: Public pages have no unreplaced template tokens
    Given I open "<page>"
    Then the page should not contain unreplaced template tokens

    # BUG: HeaderFooter/Banner write a hidden <h1> of "{0} Home - ..." with the portal
    # name placeholder never formatted in.
    @known-bug @fail
    Examples: Pages with a banner <page>
      | page                  |
      | /                     |
      | /maps                 |
      | /results/?t=a         |

    Examples: Pages without a banner <page>
      | page                  |
      | /contact              |
      | /nosuchcollectionxyz  |
