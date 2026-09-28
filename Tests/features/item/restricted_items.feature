@item
Feature: Dark, private and empty items
  Dark and private items still return a normal page to anonymous visitors, never a 404 - a
  restricted-content banner is shown, but (on this site) the citation still renders in full
  underneath it, since "Show Citation For Dark Items" is on.

  Scenario: A dark item shows a restricted banner, with its citation still rendered beneath it
    Given I open "/AA00000001/00001"
    Then the response status should be 200
    And the "restricted item notice" should contain "DARK ITEM"
    And the "citation title" should be visible

  Scenario: A private item shows a different restricted banner
    Given I open "/AA00001661/00001"
    Then the response status should be 200
    And the "restricted item notice" should contain "PRIVATE ITEM"
    And the "restricted item notice" should contain "Digitization of this item is currently in progress."

  Scenario: An item with no pages or files still renders its citation page
    Given I open "/AA00001662/00001"
    Then the response status should be 200
    And the "citation title" should be visible
    And the "restricted item notice" should not be present

  # Item_HtmlSubwriter has no "redirect anonymous visitors to logon" path for admin-only viewer
  # codes at the item level (unlike the equivalent aggregation-level management pages, which do
  # redirect) - an anonymous visitor requesting one just silently gets the normal page-image
  # viewer instead, on the very same URL, with no indication access was denied.
  @known-bug
  Scenario: An admin-only item viewer code silently falls back instead of asking anonymous visitors to log on
    Given I open "/AA00001559/00001/manage"
    Then the response status should be 200
    And I should be on "/AA00001559/00001/manage"
    And the "item menu" should be visible
