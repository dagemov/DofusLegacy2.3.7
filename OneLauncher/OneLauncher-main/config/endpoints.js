const DEFAULT_API_BASE_URL = 'https://rollblack-legacy.onesv.online';

function trimTrailingSlash(value) {
  return value.replace(/\/+$/, '');
}

function ensureTrailingSlash(value) {
  return value.endsWith('/') ? value : `${value}/`;
}

const apiBaseUrl = trimTrailingSlash(process.env.ONELAUNCHER_API_BASE_URL || DEFAULT_API_BASE_URL);
const electronUpdatesBaseUrl = ensureTrailingSlash(
  process.env.ONELAUNCHER_ELECTRON_UPDATES_URL || `${apiBaseUrl}/api/launcher/electron-updates`
);

module.exports = {
  endpoints: {
    apiBaseUrl,
    apiManifestUrl: `${apiBaseUrl}/api/launcher/manifest`,
    apiHealthUrl: `${apiBaseUrl}/api/health`,
    apiRegisterUrl: `${apiBaseUrl}/api/auth/register`,
    apiLoginUrl: `${apiBaseUrl}/api/auth/login`,
    apiCheckUsernameUrl: `${apiBaseUrl}/api/auth/check-username`,
    electronUpdatesBaseUrl,
    launcherReleasesUrl: electronUpdatesBaseUrl
  }
};
