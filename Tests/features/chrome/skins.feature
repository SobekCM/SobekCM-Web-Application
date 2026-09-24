@chrome
Feature: Web skins
  The site's web skin supplies the header, footer, stylesheet and images around every
  page. A skin can be picked with "n=code"; an unknown skin code must fail safely.

  Scenario Outline: The skin's stylesheet and design files load on every kind of page
    Given I open "<page>"
    Then the skin stylesheet should load
    And no skin or design file should fail to load

    Examples: <page>
      | page          |
      | /             |
      | /maps         |
      | /results/?t=a |
      | /maps/all     |

  # BUG: an invalid skin code returns a plain-text 404 that includes the full server trace.
  @known-bug @fail @security
  Scenario: An unknown skin code never exposes server internals
    When I request "/?n=bogusskin"
    Then the response body should not contain "queryinitializer"
    And the response should be an HTML page
