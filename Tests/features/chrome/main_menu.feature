@chrome
Feature: Collection main menu
  Every collection page has a main menu linking to the collection home, its search
  forms, its item views, and its subcollections. Results pages add view tabs.

  Scenario: The top-level home page menu
    Given I open "/"
    Then the "main menu" should be visible
    And the main menu should include "Advanced Search"
    And the main menu should include "Text Search"
    And the main menu should include "View Items"
    And the main menu should include "Rights"
    And the "Testing Home" main menu item should be selected

  Scenario: The home menu offers the list, brief and tree views of the site
    Given I open "/"
    Then the "home submenu" should contain these links:
      | text       | path   |
      | List View  | /      |
      | Brief View | /brief |
      | Tree View  | /tree  |

  Scenario: A collection's menu links to its own pages
    Given I open the "maps" collection
    Then the "main menu" should contain these links:
      | text                 | path           |
      | Maps Collection Home | /maps          |
      | Testing Home         | /              |
      | Advanced Search      | /maps/advanced |
      | Text Search          | /maps/text     |
      | View Items           | /maps/all      |

  Scenario: A collection's menu lists its subcollections
    Given I open the "maps" collection
    Then the "subcollections submenu" should list exactly these links:
      | Historic Maps |
      | Sanborn Maps  |

  Scenario: Hovering the subcollections menu item reveals the subcollections
    Given I open the "maps" collection
    When I hover over the "Subcollections" main menu item
    Then the "subcollections submenu" should be visible

  Scenario: Menu links open the matching page
    Given I open the "maps" collection
    When I click the "Advanced Search" link in the "main menu"
    Then I should be on "/maps/advanced"
    And the "advanced search panel" should be visible

  Scenario: The results menu groups the other search forms under Search Options
    Given I open "/results/?t=a"
    Then the main menu should include "Search Options"
    And the main menu should not include "Advanced Search"
    And the main menu should not include "Text Search"
    And the "search options submenu" should contain "Advanced Search"
    And the "search options submenu" should contain "Text Search"

  Scenario: The results menu offers the results views, but no map view without coordinates
    Given I open "/results/?t=a"
    Then the main menu should include "Brief View"
    And the main menu should include "Thumbnail View"
    And the main menu should include "Table View"
    And the main menu should not include "Map View"
