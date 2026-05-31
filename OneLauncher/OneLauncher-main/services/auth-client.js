const axios = require('axios');
const { endpoints } = require('../config/endpoints');

async function login(credentials) {
  const response = await axios.post(endpoints.apiLoginUrl, credentials, {
    timeout: 10000,
    headers: { Accept: 'application/json', 'Content-Type': 'application/json' }
  });

  return response.data;
}

async function register(payload) {
  const response = await axios.post(endpoints.apiRegisterUrl, payload, {
    timeout: 15000,
    headers: { Accept: 'application/json', 'Content-Type': 'application/json' }
  });

  return response.data;
}

async function checkHealth() {
  const response = await axios.get(endpoints.apiHealthUrl, {
    timeout: 2500,
    headers: { Accept: 'application/json' }
  });

  return response.data;
}

module.exports = {
  login,
  register,
  checkHealth
};
