import type { APIRequestContext } from '@playwright/test';

// Trimmed to the fields these tests actually check - see the full BriefItemInfo model in
// Code/SobekCM_Core/BriefItem/ if a scenario ever needs more of it.
export interface BriefItemInfo {
  behaviors: {
    dark?: boolean;
    ipRestriction?: number;
    textSearchable?: boolean;
  };
  web?: {
    fileExtensions?: string[];
  };
  geospatial?: {
    points?: unknown[];
    polygons?: unknown[];
  };
}

// GET /engine/items/brief/json/{bibid}/{vid} - the same BriefItemInfo the site itself renders
// from, and (unlike several other /engine/* endpoints, e.g. items/bytitle) not IP-restricted, so
// it's callable from any test runner. Lets a scenario verify a test-fixture assumption (e.g. "this
// item really is Dark") independently of what the rendered page happens to show, so a failure can
// tell "the test data changed" apart from "the code broke".
export async function getItemBrief(request: APIRequestContext, bibidVid: string): Promise<BriefItemInfo> {
  const [bibid, vid] = bibidVid.split('/');
  const response = await request.get(`/engine/items/brief/json/${bibid}/${vid}`);
  // UTF-8 with a leading byte-order mark - JSON.parse rejects that raw.
  const text = (await response.text()).replace(/^﻿/, '');
  return JSON.parse(text);
}
