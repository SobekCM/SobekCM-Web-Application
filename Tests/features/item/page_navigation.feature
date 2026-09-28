@item
Feature: Multi-page and multi-volume item navigation
  A multi-page item's table of contents shows its real division names, and clicking a
  division changes which page is current. A multi-volume item's bare BibID opens a real
  volume, and its "All Volumes" tab is reachable.

  Scenario: The table of contents shows named divisions, not just page numbers
    Given I open "/AA00001559/00001"
    Then the current table-of-contents page should be "Front Cover"
    When I click the "Front Matter" link in the "table of contents"
    Then I should be on "/AA00001559/00001/3j"
    And the current table-of-contents page should be "Front Matter"

  Scenario: A long single-volume item's table of contents still renders
    Given I open "/UF00086478/00001"
    Then the response status should be 200
    And the "table of contents" should be visible

  Scenario: A multi-volume item's bare BibID opens a real volume
    Given I open "/AA00001660"
    Then the response status should be 200
    And I should be on "/AA00001660/00003"

  # AA00001660/00049 ("Introduction", under "Module 10: Bacterial Unknowns") - the tree opens to
  # the current volume's own branch, and marks the current volume as plain text, not a link,
  # unlike every other volume in the tree.
  Scenario: All Volumes opens the tree to the current volume, marked as text rather than a link
    Given I open "/AA00001660/00049/allvolumes"
    Then the response status should be 200
    And the "volumes tree" should contain "Module 10 : Bacterial Unknowns"
    And the current volume in the tree should not be a link
    And the "Exercise 10.1 : Identification of Bacterial Unknowns" link should be visible

  Scenario: A collapsed branch expands to reveal its volumes
    Given I open "/AA00001660/00049/allvolumes"
    Then the "Exercise 1.2 : Night on the Town" link should not be visible
    When I expand the "Module 1 : Introduction & Safety" branch in the "volumes tree"
    Then the "Exercise 1.2 : Night on the Town" link should be visible

  Scenario: Clicking a volume in the tree opens that item
    Given I open "/AA00001660/00049/allvolumes"
    When I click the "Exercise 10.1 : Identification of Bacterial Unknowns" link in the "volumes tree"
    Then I should be on "/AA00001660/00050"
