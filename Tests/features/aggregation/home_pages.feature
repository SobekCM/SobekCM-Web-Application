@aggregation
Feature: Collection home pages
  Visitors who are not logged on can browse the site's collections, starting from the
  top-level home page, and open each collection's own home page.

  Scenario: The top-level home page lists every collection and institution
    Given I open "/"
    Then the response status should be 200
    And the page title should be "Testing Home - All Collection Groups"
    And the "collection list" should contain these links:
      | text                             | path            |
      | Aerial Photography Collection    | /aerials        |
      | Children's Literature Collection | /juvenile       |
      | Egyptian Collection              | /egypt          |
      | Fine Art Collection              | /fine-art       |
      | Maps Collection                  | /maps           |
      | Museum Artifact Collection       | /museum         |
      | Cleveland Museum of Art          | /icleveland-art |
      | David Rumsey Map Collection      | /irumsey-maps   |
      | Penn Museum                      | /ipenn-museum   |
      | Rijksmuseum                      | /irijksmuseum   |
    And the page should have no script errors

  Scenario Outline: Each collection home page opens from its short code
    Given I open the "<code>" collection
    Then the response status should be 200
    And the page title should be "Testing Home - <name>"
    And the "search box" should be visible
    And the page should have no script errors

    Examples: <code>
      | code           | name                             |
      | aerials        | Aerial Photography Collection    |
      | juvenile       | Children's Literature Collection |
      | egypt          | Egyptian Collection              |
      | fine-art       | Fine Art Collection              |
      | maps           | Maps Collection                  |
      | museum         | Museum Artifact Collection       |
      | historic-maps  | Historic Maps                    |
      | sanborn-maps   | Sanborn Maps                     |
      | icleveland-art | Cleveland Museum of Art          |
      | irijksmuseum   | Rijksmuseum                      |

  Scenario: Clicking a collection on the top-level home page opens that collection
    Given I open "/"
    When I click the "Maps Collection" link in the "collection list"
    Then I should be on "/maps"
    And the page title should be "Testing Home - Maps Collection"

  Scenario: The top-level home page can be shown as a list, brief descriptions, or a tree
    Given I open "/"
    Then the "home view tabs" should contain "LIST VIEW"
    When I click the "BRIEF VIEW" link in the "home view tabs"
    Then I should be on "/brief"
    And the "collection description table" should be visible
    When I click the "TREE VIEW" link in the "home view tabs"
    Then I should be on "/tree"
    And the "collection tree" should be visible

  Scenario: The tree view can expand to show every collection
    Given I open "/tree"
    When I click the "Expand All" link without leaving the page
    Then the "Sanborn Maps" link should be visible
    And the "Historic Maps" link should be visible

  Scenario: The partners page lists only institutions
    Given I open "/partners"
    Then the response status should be 200
    And I should see "Rijksmuseum"
    And I should see "Penn Museum"
    And I should not see "Egyptian Collection"

  Scenario: A collection home page lists its subcollections
    Given I open the "maps" collection
    Then the "collection list" should contain "Subcollections"
    And the "collection list" should contain these links:
      | text          | path           |
      | Historic Maps | /historic-maps |
      | Sanborn Maps  | /sanborn-maps  |
    When I click the "Sanborn Maps" link in the "collection list"
    Then I should be on "/sanborn-maps"

  Scenario: A hidden or inactive sub-collection is not listed on the parent's home page, menu or tree
    Given I open the "maps" collection
    Then the "collection list" should not contain "Hidden Maps"
    And the "collection list" should not contain "Inactive Maps"
    And the "subcollections submenu" should not contain "Hidden Maps"
    And the "subcollections submenu" should not contain "Inactive Maps"
    When I open "/tree"
    Then I should not see "Hidden Maps"
    And I should not see "Inactive Maps"

  # By design, not a bug: both render normally by direct URL. Inactive collections are meant to
  # stay directly reachable this way - it lets collection staff share and review a collection
  # before it's made active/published.
  Scenario Outline: A hidden or inactive collection still opens fine by direct URL
    Given I open the "<code>" collection
    Then the response status should be 200
    And the page title should be "Testing Home - <name>"

    Examples: <code>
      | code          | name                     |
      | hidden-maps   | Hidden Maps Collection   |
      | inactive-maps | Inactive Maps Collection |

  Scenario: An inactive collection with no search enabled shows its home page with no search box
    Given I open the "inactive-maps" collection
    Then I should see "Inactive Maps Collection Home"
    And the "search box" should not be present

  Scenario: A collection alias renders the real collection and keeps the alias in its own links
    Given I open "/spatial"
    Then the response status should be 200
    And the page title should be "Testing Home - Maps Collection"
    And I should be on "/spatial"
    When I search for "egypt"
    Then I should be on "/spatial/results/?t=egypt"

  Scenario: Home text placeholders are replaced with real item, title and page counts
    Given I open the "newspapers" collection
    Then I should see "This small sample contains 25 items over 4 newspaper titles totaling 116 pages."
    And the page should not contain unreplaced template tokens

  Scenario: The thumbnails home page shows a sample and a link to view all
    Given I open the "postcards" collection
    Then I should see "Showing 20 items out of 96"
    When I click the "View all" link
    Then I should be on "/postcards/all"

  Scenario: The personalized home page falls back to the list view for anonymous visitors
    Given I open "/personalized"
    Then the response status should be 200
    And the "collection list" should contain "Maps Collection"

  Scenario: A collection's edit page quietly shows the normal page to anonymous visitors
    Given I open "/maps/edit"
    Then the page title should be "Testing Home - Maps Collection"
    And I should not see "edit content"

  Scenario: An unknown page inside a collection falls back to the collection home
    Given I open "/maps/nosuchchildpage"
    Then the response status should be 200
    And the page title should be "Testing Home - Maps Collection"

  Scenario: An unknown collection code shows the Page Not Found page
    Given I open "/nosuchcollectionxyz"
    Then the response status should be 404
    And the "page not found panel" should contain "Page Not Found"
    And the "footer" should be visible

  Scenario: A static information page opens from the main menu
    Given I open "/"
    When I click the "Rights" link in the "main menu"
    Then I should be on "/info/rights"
    And the page title should be "Testing - Rights - All Collection Groups"
    And the "Rights" main menu item should be selected

  Scenario Outline: A static child page shows its own heading, content and menu tab
    Given I open "<path>"
    Then the response status should be 200
    And the page title should be "<title>"
    And the visible page heading should be "<heading>"
    And the "static page text" should be visible
    And the "<heading>" main menu item should be selected

    Examples: <path>
      | path              | title                                                 | heading               |
      | /info/rights      | Testing - Rights - All Collection Groups              | Rights                |
      | /newspapers/about | Testing - About the Newspapers - Newspaper Collection | About the Newspapers  |

  Scenario: The empty view renders only an empty placeholder
    Given I open "/maps/empty"
    Then the response status should be 200
    And the "empty page marker" should be present
