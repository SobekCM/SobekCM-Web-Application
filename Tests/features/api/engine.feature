@api
Feature: Engine endpoints
  The /engine endpoints answer in the same web application. Bad requests get clear
  errors that never expose server internals.

  Scenario: Requesting the engine with no endpoint is a bad request
    When I request "/engine/"
    Then the HTTP status should be 400
    And the response body should contain "No endpoint requested"

  Scenario: An unknown engine endpoint is reported as not found
    When I request "/engine/nosuchendpoint/json"
    Then the HTTP status should be 501
    And the response body should contain "No endpoint found"

  # BUG: the 501 response for an unknown endpoint includes the whole configuration-loading
  # log: server directories, plugin names and timings.
  @known-bug @fail @security
  Scenario: An unknown engine endpoint does not expose the configuration log
    When I request "/engine/nosuchendpoint/json"
    Then the response body should not contain "Beginning to read configuration files"

  # BUG: ResultsServices has no public methods behind the configured search endpoints.
  @known-bug @fail
  Scenario: The search results endpoint returns results
    When I request "/engine/search/results/json?t=map"
    Then the HTTP status should be 200
