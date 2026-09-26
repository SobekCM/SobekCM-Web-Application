@maps
Feature: Map browse and map search
  Collections with geographic data can be browsed and searched on a map.
  Map browse only shows item-level points, so a collection whose coordinates are all
  polygons (like the aerials) has nothing to show there. These pages need a Google Maps
  API key in the site settings.

  Scenario: Without a Google Maps key, map pages explain that maps are unavailable
    Given Google Maps is not enabled on this site
    And I open "/aerials/geography"
    Then the response status should be 200
    And I should see "Google Maps are not enabled"
    And the page should have no script errors
