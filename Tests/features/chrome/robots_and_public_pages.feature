@chrome
Feature: Search engines and other public pages
  Search engine robots get simplified pages. The contact form and robots.txt are public.

  Scenario: robots.txt is served
    When I request "/robots.txt"
    Then the HTTP status should be 200
    And the response body should contain "User-agent"

  # BUG: SearchEngineRobotNavigationInitializer redirects robots on a results page to a URL
  # rebuilt from the unchanged request, i.e. the same URL, forever.
  @known-bug @fail
  Scenario: A search engine robot on a results page is not redirected in a loop
    When a search engine robot requests "/results/?t=map"
    Then the response should not redirect back to the same address

  Scenario: A robot on a collection home page gets the page without language or account links
    When a search engine robot requests "/maps"
    Then the HTTP status should be 200
    And the response body should not contain "?l=fr"
    And the response body should not contain "myTesting"

  Scenario: The contact form is offered to anonymous visitors
    Given I open "/contact"
    Then the page title should be "Testing Contact Us"
    And the "contact form" should be visible
    And I should see "Enter a subject here:"
    And I should see "Enter your e-mail address here:"

  Scenario: The contact form can be opened for a specific collection
    Given I open "/contact/maps"
    Then the response status should be 200
    And the "contact form" should be visible

  Scenario: Cancelling the contact form returns to the home page
    Given I open "/contact"
    When I click the "Cancel" contact form button
    Then I should be on "/"
