const DEFAULT_API_BASE_URL = 'https://rollblack-legacy.onesv.online';
const DEFAULT_LEGACY_UPDATE_BASE_URL = 'https://beta.1emu.fun/updates/';

function trimTrailingSlash(value) {
  return value.replace(/\/+$/, '');
}

function ensureTrailingSlash(value) {
  return value.endsWith('/') ? value : `${value}/`;
}

const apiBaseUrl = trimTrailingSlash(process.env.ONELAUNCHER_API_BASE_URL || DEFAULT_API_BASE_URL);
const legacyUpdateBaseUrl = ensureTrailingSlash(
  process.env.ONELAUNCHER_LEGACY_UPDATE_BASE_URL || DEFAULT_LEGACY_UPDATE_BASE_URL
);

module.exports = {
  endpoints: {
    apiBaseUrl,
    apiManifestUrl: `${apiBaseUrl}/api/launcher/manifest`,
    apiHealthUrl: `${apiBaseUrl}/api/health`,
    apiRegisterUrl: `${apiBaseUrl}/api/auth/register`,
    apiLoginUrl: `${apiBaseUrl}/api/auth/login`,
    apiCheckUsernameUrl: `${apiBaseUrl}/api/auth/check-username`,
    legacyUpdateBaseUrl,
    legacyUpdatesXmlUrl: `${legacyUpdateBaseUrl}Updates.xml`
  }
};
