@chrome @i18n
Feature: Choosing the interface language
  Visitors pick a language from the footer. "l=xx" switches the language and remembers it
  for the rest of the visit; "lo=xx" switches it for one page only. Languages the site
  isn't configured for are ignored.

  Scenario: Choosing a language from the footer translates the page
    Given I open the "maps" collection
    When I switch the language to "French"
    Then the page language should be "fr"
    And the "search prompt" should contain "Recherche dans la collection:"
    And the main menu should include "Recherche Avancée"

  Scenario: A language chosen from the footer is remembered on later pages
    Given I open "/?l=fr"
    When I open "/maps"
    Then the page language should be "fr"
    And the URL should not contain "l=fr"

  Scenario: A one-page language does not change later pages
    Given I open "/maps?lo=de"
    Then the page language should be "de"
    When I open "/maps"
    Then the page language should be "en"

  Scenario: An unknown language code is ignored
    Given I open "/maps?l=xx"
    Then the response status should be 200
    And the page language should be "en"
    And the "search prompt" should contain "Search Collection:"

  Scenario Outline: The results page is translated
    Given I open "/results/?t=a&lo=<code>"
    Then the page language should be "<code>"
    And the "facet column" should contain "<facet title>"
    And the "print button" should contain "<print>"

    Examples: <code>
      | code | facet title                | print    |
      | fr   | RAFFINEZ                   | Imprimer |
      | de   | ERGEBNISSE EINGRENZEN NACH | Drucken  |
      | es   | LIMITAR RESULTADOS POR     | Imprimir |

  Scenario: French results show the translated range text and sort options
    Given I open "/results/?t=a&lo=fr"
    Then the result range should read "1 - 20 de 29 titres correspondants"
    And the "sort dropdown" should contain "Pertinence"
    And the search explanation should contain "Votre recherche dans"

  # BUG: the subcollections menu item and section heading are not translated.
  @known-bug @fail
  Scenario: The subcollections label is translated
    Given I open "/maps?lo=fr"
    Then the "main menu" should not contain "Subcollections"
