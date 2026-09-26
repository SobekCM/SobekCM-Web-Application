@aggregation @security
Feature: Collection management pages are closed to anonymous visitors
  Usage statistics, in-process items, and collection management pages are for logged-on
  staff only. An anonymous visitor asking for one is sent away without seeing any of it.

  Scenario Outline: An anonymous visitor is sent away from a collection management page
    When I request "/maps/<page>"
    Then the response should redirect to "/maps"

    Examples: <page>
      | page        |
      | usage       |
      | inprocess   |
      | manage      |
      | permissions |
      | history     |

  Scenario: The system administration page asks anonymous visitors to log on
    When I request "/admin"
    Then the response should redirect to "/my/logon"

  Scenario Outline: Staff-only pages ask anonymous visitors to log on
    Given I open "<page>"
    Then I should see "The feature you are trying to access requires a valid logon."

    Examples: <page>
      | page            |
      | /internal       |
      | /my/preferences |

  Scenario: The logged-on version of a page redirects anonymous visitors to the public page
    When I request "/l/maps"
    Then the response should redirect to "/maps"

  Scenario: Anonymous visitors never see admin or personal menu items
    Given I open the "maps" collection
    Then the anonymous visitor should not see any admin menu items

  # The counts include items still in process or not yet public (fixed 2026-09-26: it used to
  # have no logon check at all)
  Scenario: The item count page is closed to anonymous visitors
    Given I open "/maps/itemcount"
    Then I should not see "Resource Count in Collection"
