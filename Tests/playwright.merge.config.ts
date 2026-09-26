// Used only by `playwright merge-reports`, to combine the blob reports the e2e pipeline's stage-by-stage runs write
// (one per stage, see playwright.config.ts) into a single HTML report and a single JUnit file -- the one the
// pipeline uploads for the monitoring database.
export default {
  testDir: 'features',
  reporter: [
    ['junit', { outputFile: 'test-results/junit.xml' }],
    ['html', { open: 'never' }],
  ],
};
