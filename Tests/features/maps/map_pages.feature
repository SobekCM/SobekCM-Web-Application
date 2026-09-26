@maps
Feature: Map browse and map search
  Collections with geographic data can be browsed and searched on a map.
  Map browse only shows item-level points, so a collection whose coordinates are all
  polygons (like the aerials) has nothing to show there. Map search results are sorted
  smallest footprint first: points share one map at the top, then each area gets a map
  of its own, from a neighbourhood out to a whole state.

  Most scenarios use a stand-in for Google Maps ("Google Maps is stubbed"), which records
  what the page asks Google to draw without needing a key or the network. The data is the
  testing site's: the aerial photography flights over Alachua County, and the Gainesville
  postcards.

  Scenario: Without a Google Maps key, map pages explain that maps are unavailable
    Given Google Maps is not enabled on this site
    And I open "/aerials/geography"
    Then the response status should be 200
    And I should see "Google Maps are not enabled"
    And the page should have no script errors

  # ----- Map search results -----

  Scenario: A map search opens in the map view, which the menu offers even where the collection doesn't list it
    Given Google Maps is stubbed
    When I open "/aerials/results/?coord=29.655361721649527,-82.34009702669194,,"
    Then the main menu should include "Map View"
    And the page should have no script errors

  Scenario: Each matching aerial flight is drawn as its own area, smallest first
    Given Google Maps is stubbed
    When I open "/aerials/results/?coord=29.655361721649527,-82.34009702669194,,"
    Then the map results should draw 3 maps
    And each map should draw one area
    And the areas should be listed from smallest to largest
    And the page should have no script errors

  # Page one of this search is ten points (A to J), then areas. The viewer once fell back to A after the
  # tenth point, so a page with more than ten points would show a second A; this data now stops at J.
  Scenario: Point results share one map, with a letter for each point that matches the list
    Given Google Maps is stubbed
    When I open "/postcards/results/?coord=29.74634520620229,-82.5020802620917,29.517156307102727,-82.1175587777167"
    Then the point results should share one map
    And the point markers should be lettered in order, with no letter repeated
    And each point marker should appear beside its titles in the list
    And the page should have no script errors

  Scenario: Clicking a point marker brings its titles into view
    Given Google Maps is stubbed
    When I open "/postcards/results/?coord=29.74634520620229,-82.5020802620917,29.517156307102727,-82.1175587777167"
    And I click point marker "C"
    Then the titles at point marker "C" should be scrolled into view and highlighted

  Scenario: Where a search moves from points to areas, the points come first and the areas grow
    Given Google Maps is stubbed
    When I open "/results/map/3/?coord=29.88528064117508,-82.69012716412544,29.52265928138108,-81.96502950787544"
    Then the points should come before the areas
    And the areas should be listed from smallest to largest
    And the page should have no script errors

  # ----- Map browse -----

  Scenario: Map browse shows a collection's item-level points
    Given Google Maps is enabled on this site
    And Google Maps is stubbed
    When I open "/postcards/geography"
    Then the map browse should show the collection's points
    And the page should have no script errors

  # The aerials only have coordinates at the page level (one polygon per tile), and map browse
  # deliberately shows item-level points only
  Scenario: Map browse of a collection with only areas shows no points
    Given Google Maps is enabled on this site
    And Google Maps is stubbed
    When I open "/aerials/geography"
    Then the map browse should show no points
    And the page should have no script errors

  # ----- The real Google Maps -----

  @real-google
  Scenario: With the site's real key, Google draws the map search results
    Given Google Maps is enabled on this site
    When I open "/aerials/results/?coord=29.655361721649527,-82.34009702669194,,"
    Then Google should draw the map
    And the page should have no script errors
