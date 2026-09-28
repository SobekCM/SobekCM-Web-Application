@item
Feature: Dark, private and empty items
  Dark and private items still return a normal page to anonymous visitors, never a 404 - a
  restricted-content banner is shown, but (on this site) the citation still renders in full
  underneath it, since "Show Citation For Dark Items" is on.

  # The Given here hits the engine's own item data directly (not the rendered page) to confirm
  # this item really is still Dark before testing what Dark items look like - so if the test data
  # ever drifts, this scenario fails on that first line with a clear reason, not on a confusing
  # mismatch further down.
  Scenario: A dark item shows a restricted banner, with its citation still rendered beneath it
    Given the item "AA00000001/00001" is marked Dark
    When I open "/AA00000001/00001"
    Then the response status should be 200
    And the "restricted item notice" should contain "DARK ITEM"
    And the "citation title" should be visible

  # Downloads_ItemViewer excludes itself for Dark items - hitting /downloads directly on a dark
  # item falls back to the citation viewer (with its restricted banner) instead of listing files.
  Scenario: A dark item never shows its attached files, even by direct URL
    Given the item "AA00000001/00001" is marked Dark
    When I open "/AA00000001/00001"
    Then the "item menu" should not contain "Downloads"
    When I open "/AA00000001/00001/downloads"
    Then the response status should be 200
    And the "restricted item notice" should contain "DARK ITEM"
    And the "downloads viewer" should not be present

  Scenario: A private item shows a different restricted banner
    Given the item "AA00001661/00001" is marked Private
    When I open "/AA00001661/00001"
    Then the response status should be 200
    And the "restricted item notice" should contain "PRIVATE ITEM"
    And the "restricted item notice" should contain "Digitization of this item is currently in progress."

  # Unlike Dark, Private items still show their attached files - Downloads_ItemViewer's exclusion
  # is specific to the Dark flag, not the broader "restricted" state private items are also in.
  # The restricted-item wording differs here too: "only available as the following downloads"
  # rather than the plain "has the following downloads" a non-restricted item gets.
  Scenario: A private item still shows its attached files
    Given the item "AA00001661/00001" is marked Private
    And the item "AA00001661/00001" has a "pdf" file
    When I open "/AA00001661/00001"
    Then the "item menu" should contain "Downloads"
    When I open "/AA00001661/00001/downloads"
    Then the "restricted item notice" should contain "PRIVATE ITEM"
    And the "downloads viewer" should contain "This item is only available as the following downloads:"
    And I should see "( .pdf )"

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
