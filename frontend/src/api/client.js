import axios from 'axios';
import { STORAGE_KEYS } from '../constants';

// Base URL is proxied to the backend via Vite in dev; overridable for other environments.
const baseURL = import.meta.env.VITE_API_BASE_URL || '/api';

const client = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
});

export const tokenStore = {
  getAccess: () => localStorage.getItem(STORAGE_KEYS.ACCESS_TOKEN),
  getRefresh: () => localStorage.getItem(STORAGE_KEYS.REFRESH_TOKEN),
  set: (access, refresh) => {
    if (access) localStorage.setItem(STORAGE_KEYS.ACCESS_TOKEN, access);
    if (refresh) localStorage.setItem(STORAGE_KEYS.REFRESH_TOKEN, refresh);
  },
  clear: () => {
    localStorage.removeItem(STORAGE_KEYS.ACCESS_TOKEN);
    localStorage.removeItem(STORAGE_KEYS.REFRESH_TOKEN);
    localStorage.removeItem(STORAGE_KEYS.USER);
  },
};

// Attach the access token to every request.
client.interceptors.request.use((config) => {
  const token = tokenStore.getAccess();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// Transparent refresh-token rotation on 401. Concurrent 401s share one refresh call.
let refreshing = null;
let onAuthFailure = null;

export const setAuthFailureHandler = (handler) => { onAuthFailure = handler; };

client.interceptors.response.use(
  (response) => response,
  async (error) => {
    const { config, response } = error;
    if (response?.status === 401 && !config._retried && tokenStore.getRefresh()) {
      config._retried = true;
      try {
        refreshing = refreshing || axios.post(`${baseURL}/auth/refresh`, {
          refreshToken: tokenStore.getRefresh(),
        });
        const { data } = await refreshing;
        refreshing = null;
        tokenStore.set(data.accessToken, data.refreshToken);
        config.headers.Authorization = `Bearer ${data.accessToken}`;
        return client(config);
      } catch (refreshError) {
        refreshing = null;
        tokenStore.clear();
        if (onAuthFailure) onAuthFailure();
        return Promise.reject(refreshError);
      }
    }
    return Promise.reject(error);
  },
);

// Extracts a human-readable message from an API error response.
export const getErrorMessage = (error, fallback = 'Something went wrong.') => {
  const data = error?.response?.data;
  if (!data) return error?.message || fallback;
  if (data.errors) {
    const first = Object.values(data.errors)[0];
    if (Array.isArray(first) && first.length) return first[0];
  }
  return data.title || data.message || fallback;
};

export default client;
