@chrome
Feature: Search engines and other public pages
  Search engine robots get simplified pages. The contact form and robots.txt are public.
  A site can also keep every robot out (Robots:BlockAll in appsettings.json, as on the demo and
  testing sites); the robot scenarios below say which kind of site they need.

  Scenario: A site that allows robots serves its own robots.txt file
    Given a site that allows robots
    When a search engine robot requests "/robots.txt"
    Then the HTTP status should be 200
    And the response body should contain "User-agent"
    And the robots.txt should be the site's own file

  # A 403 or 404 on robots.txt tells a crawler there are no rules at all, so a site that blocks
  # every robot has to answer this one request from a robot, and say Disallow -- whatever the
  # robots.txt file on disk says, or even if there isn't one
  Scenario: A site that blocks all robots tells them so in robots.txt
    Given a site that blocks all robots
    When a search engine robot requests "/robots.txt"
    Then the HTTP status should be 200
    And the robots.txt should be the one SobekCM generates
    And the response body should contain "Disallow: /"

  Scenario: A site that blocks all robots refuses their page requests
    Given a site that blocks all robots
    When a search engine robot requests "/"
    Then the HTTP status should be 403

  # BUG: SearchEngineRobotNavigationInitializer redirects robots on a results page to a URL
  # rebuilt from the unchanged request, i.e. the same URL, forever.
  @known-bug @fail
  Scenario: A search engine robot on a results page is not redirected in a loop
    Given a site that allows robots
    When a search engine robot requests "/results/?t=map"
    Then the response should not redirect back to the same address

  Scenario: A robot on a collection home page gets the page without language or account links
    Given a site that allows robots
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
