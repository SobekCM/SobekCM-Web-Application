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

  # An unknown skin code falls back to the site's default skin (fixed 2026-09-26: it used to
  # return a plain-text 404 that included the full server trace)
  @security
  Scenario: An unknown skin code never exposes server internals
    When I request "/?n=bogusskin"
    Then the response body should not contain "queryinitializer"
    And the response should be an HTML page

  # The testing site has exactly two web skins: the main skin and "altskin". n=altskin is a sticky
  # override that self-propagates onto every generated link and form on the page.
  Scenario: Requesting an alternate skin switches the stylesheet and stays selected on later links
    Given I open "/text?n=altskin"
    Then the response status should be 200
    And the page HTML should contain "design/skins/ALTSKIN/ALTSKIN.css"
    When I search for "map"
    Then the URL should contain "n=altskin"

  # "inactive-maps" is configured to only ever show under "altskin". Visiting it under the site's
  # main skin (no n=, or an n= it doesn't recognize) quietly renders altskin instead of erroring or
  # ignoring the restriction - and that has to hold on its results/browse pages too, not just its home.
  Scenario: A collection restricted to one skin always uses that skin, including on its browse page
    Given I open the "inactive-maps" collection
    Then the page HTML should contain "design/skins/ALTSKIN/ALTSKIN.css"
    When I open "/inactive-maps?n=default"
    Then the page HTML should contain "design/skins/ALTSKIN/ALTSKIN.css"
    When I open "/inactive-maps/all"
    Then the response status should be 200
    And the page HTML should contain "design/skins/ALTSKIN/ALTSKIN.css"
