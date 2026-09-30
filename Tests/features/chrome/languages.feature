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
    Then the result range should read "1 - 20 de {total} titres correspondants"
    And the "sort dropdown" should contain "Pertinence"
    And the search explanation should contain "Votre recherche dans"

  # Fixed 2026-09-26: the French dictionary "translated" Subcollections as "Subcollections"
  Scenario: The subcollections label is translated
    Given I open "/maps?lo=fr"
    Then the "main menu" should not contain "Subcollections"

  # The front page's web skin has real per-language content, not just translated interface labels:
  # a skin-authored tagline in the header, and a distinct banner image file per language.
  Scenario Outline: The front page header tagline and banner are shown in the visitor's language
    Given I open "/?lo=<code>"
    Then I should see "<tagline>"
    And the page HTML should contain "<banner file>"

    Examples: <code>
      | code | tagline                                  | banner file         |
      | en   | Automated Testing Infrastructure         | testing_en_1200.png |
      | es   | Infraestructura de pruebas automatizadas | testing_es_1200.png |
      | fr   | Infrastructure de tests automatisés      | testing_fr_1200.png |
      | nl   | Geautomatiseerde Testinfrastructuur      | testing_nl_1200.png |
      | de   | Automatisierte Testinfrastruktur         | testing_de_1200.png |

  # Italian is enabled on this site but has no header/banner of its own configured (unlike the five
  # languages above), so it should fall back to the default English header and banner.
  Scenario: A language with no header of its own configured falls back to the default header and banner
    Given I open "/?lo=it"
    Then I should see "Automated Testing Infrastructure"
    And the page HTML should contain "testing_en_1200.png"

  # The top-level home page shows its collections as a list, brief descriptions or a tree.
  # Fixed 2026-09-29: only the list view was translated; the brief and tree views were all English.
  # Collection names come from the testing site's own config/user translations (Italian and
  # Portuguese have none there, so they aren't listed). Collection descriptions aren't translated yet.
  Scenario Outline: The home page's brief view shows collection names in the visitor's language
    Given I open "/brief?lo=<code>"
    Then the page language should be "<code>"
    And the "home view tabs" should contain "<brief view>"
    And the "collection description table" should contain "<maps>"
    And the "collection description table" should not contain "Maps Collection"
    And the "collection list" should contain "<institutions>"

    Examples: <code>
      | code | brief view        | maps                 | institutions  |
      | fr   | VUE BRÈVE         | Collection de cartes | Institutions  |
      | es   | VISTA BREVE       | Colección de mapas   | Instituciones |
      | de   | KURZANSICHT       | Kartensammlung       | Institutionen |
      | nl   | BEKNOPTE WEERGAVE | Kaartencollectie     | Instellingen  |

  Scenario Outline: The home page's tree view is shown in the visitor's language
    Given I open "/tree?lo=<code>"
    Then the page language should be "<code>"
    And I should see "<all collections>"
    And the "tree expand links" should contain "<expand all>"
    And the "tree expand links" should contain "<collapse all>"
    And the "collection tree" should contain "<hierarchy>"
    And the "collection tree" should contain "<institutions>"
    And the "collection tree" should contain "<maps>"
    And the "collection tree" should not contain "Collection Hierarchy"
    And the "collection tree" should not contain "Maps Collection"

    Examples: <code>
      | code | all collections        | expand all      | collapse all      | hierarchy                  | institutions  | maps                 |
      | fr   | Toutes les collections | Tout développer | Tout réduire      | Hiérarchie des collections | Institutions  | Collection de cartes |
      | es   | Todas las colecciones  | Expandir todo   | Contraer todo     | Jerarquía de colecciones   | Instituciones | Colección de mapas   |
      | de   | Alle Sammlungen        | Alle ausklappen | Alle einklappen   | Sammlungshierarchie        | Institutionen | Kartensammlung       |
      | nl   | Alle collecties        | Alles uitvouwen | Alles samenvouwen | Collectiehiërarchie        | Instellingen  | Kaartencollectie     |

  # The tree's labels are translated, but the Expand/Collapse links must still work
  Scenario: The translated tree view can expand and collapse
    Given I open "/tree?lo=fr"
    When I click the "Tout développer" link without leaving the page
    Then the "Sanborn Maps" link should be visible
    When I click the "Tout réduire" link without leaving the page
    Then the "Sanborn Maps" link should not be visible
