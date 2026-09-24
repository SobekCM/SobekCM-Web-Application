@maps
Feature: Map browse and map search
  Collections with geographic data (the aerials collection) can be browsed on a map.
  These pages need a Google Maps API key in the site settings.

  Scenario: Without a Google Maps key, map pages explain that maps are unavailable
    Given Google Maps is not enabled on this site
    And I open "/aerials/geography"
    Then the response status should be 200
    And I should see "Google Maps are not enabled"
    And the page should have no script errors

  Scenario: The map browse shows the aerial flights on a map
    Given Google Maps is enabled on this site
    And I open "/aerials/geography"
    Then I should not see "Google Maps are not enabled"
    And I should see "Click here for more information about these"
    And the page should have no script errors
