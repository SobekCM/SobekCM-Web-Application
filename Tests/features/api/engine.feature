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

  # Fixed 2026-09-26: the 501 response used to include the whole configuration-loading log
  # (server directories, plugin names and timings). A Debug build of the site still shows it,
  # for development, so this scenario expects a Release build.
  @security
  Scenario: An unknown engine endpoint does not expose the configuration log
    When I request "/engine/nosuchendpoint/json"
    Then the response body should not contain "Beginning to read configuration files"

  # The engine's search endpoints were retired (2026-09-26): their methods went with the old
  # database search in July, and the web site searches Solr directly. They now report "not found"
  # like any unknown endpoint, rather than a 500 "No Method Found".
  Scenario Outline: The retired search endpoints are reported as not found
    When I request "<endpoint>"
    Then the HTTP status should be 501
    And the response body should contain "No endpoint found"

    Examples:
      | endpoint                            |
      | /engine/search/results/json?t=map   |
      | /engine/search/legacy/json?t=map    |
      | /engine/search/stats/json?t=map     |

  # Fixed 2026-09-26: the endpoint method took a Dictionary where the engine passes every endpoint
  # a NameValueCollection, so every call failed with a 500
  Scenario: The URL resolver turns a site path into its navigation details
    When I request "/engine/url-resolver/json?urlrelative=maps"
    Then the HTTP status should be 200
    And the response body should contain "maps"
