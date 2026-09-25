// Plain-language names for page regions, so feature files never contain CSS selectors.
// Selectors are the stable ids/classes the SobekCM HtmlSubwriters and helpers write.
export const regions: Record<string, string> = {
  // Chrome (HeaderFooter_HtmlHelper, skin header/footer)
  'header': 'header, #container-inner > header',
  'footer': 'footer',
  'banner': '#sbkHmw_BannerDiv',
  'banner image': '#sbkHmw_BannerDiv img#mainBanner',
  'breadcrumbs': '#instheaderbottomleft',
  'language links': '#instfooternav',
  'main content': '#main-content',

  // Main menus (MainMenus_HtmlHelper)
  'main menu': 'nav#sbkAgm_MenuBar',
  'home submenu': '#sbkAgm_HomeSubMenu, #sbkAgm_InstanceHomeSubMenu',
  'search options submenu': '#sbkAgm_SearchSubMenu',
  'subcollections submenu': '#sbkAgm_SubCollectionsMenu',

  // Aggregation home (Aggregation_HtmlSubwriter + viewers)
  'home text': '#sbkAghsw_Home',
  'home view tabs': '#sbkAghsw_HomeTypeLinks',
  'collection list': '#sbkAghsw_Children',
  'collection description table': '#sbkAghsw_CollectionDescriptionTbl',
  'collection tree': '#aggregationTree',
  'search box': '#SobekHomeSearchBox, #SobekHomeBannerSearchBox',
  'search prompt': '#sbkBsav_SearchPrompt',
  'basic search panel': '#sbkBsav_SearchPanel',
  'advanced search panel': '#sbkAsav_SearchPanel',
  'full text search panel': '#sbkFtsav_SearchPanel',
  'empty page marker': 'div#empty',
  'share form': '#share_form',

  // Results (Search_Results_HtmlSubwriter, PagedResults_HtmlHelper, ResultsViewers)
  'results explanation': '.sbkPrsw_ResultsExplanation, .sbkPrsw_DescPanel',
  'results table': '#sbkPrsw_ResultsOuterTable',
  'facet column': 'nav.sbkPrsw_FacetColumn',
  'sort dropdown': '#sorter_input',
  'view icons': '.sbkPrsw_ViewIconButtons',
  'brief results': 'section.sbkBrv_Results',
  'no results message': '.SobekNoResultsText',
  'highlighted search text': '.sbkBrv_SearchResultSnippet .texthighlight',
  'print button': '#printbutton',
  'send button': '#sendbutton',
  'save button': '#savebutton',
  'share button': '#sharebutton',

  // Other public pages
  'page not found panel': '#sbkWchs_InnerPanel',
  'contact form': 'form[name=email_form]',
  'turnstile widget': '.cf-turnstile',
  'logon form': '#form_logon',
};

export function region(name: string): string {
  const selector = regions[name.toLowerCase()];
  if (!selector) {
    throw new Error(`Unknown page region "${name}". Add it to support/regions.ts. Known: ${Object.keys(regions).join(', ')}`);
  }
  return selector;
}
