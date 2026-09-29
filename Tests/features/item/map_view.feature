@item
Feature: An item's map view after a coordinate search
  Following a coordinate search into an item, with no particular page or view asked for, opens
  the item on its map view, where the matching sheets are highlighted. The map tab keeps its
  usual "Map It!" name, so it's clear this is the same view the item always offers.

  These scenarios check the view the server picks, not the map drawing itself, so they don't
  need Google Maps.

  Background:
    Given the item "DR00000142/00001" has geographic data

  Scenario: Arriving from a coordinate search opens the item on its map view
    Given I open "/DR00000142/00001?coord=29.52634441183928,-83.09762646569658,,"
    Then the response status should be 200
    And the selected item tab should be "Map It!"
    And the "item map" should be present

  Scenario: Without a coordinate search, the item opens on its usual view
    Given I open "/DR00000142/00001"
    Then the response status should be 200
    And the "item map" should not be present

  Scenario: A coordinate search link to a particular view still opens that view
    Given I open "/DR00000142/00001/citation?coord=29.52634441183928,-83.09762646569658,,"
    Then the response status should be 200
    And the "item map" should not be present
