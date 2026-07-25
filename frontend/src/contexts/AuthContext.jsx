import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { authApi } from '../api';
import { tokenStore, setAuthFailureHandler } from '../api/client';
import { STORAGE_KEYS } from '../constants';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => {
    const raw = localStorage.getItem(STORAGE_KEYS.USER);
    return raw ? JSON.parse(raw) : null;
  });
  const [loading, setLoading] = useState(true);

  const persistUser = useCallback((u) => {
    setUser(u);
    if (u) localStorage.setItem(STORAGE_KEYS.USER, JSON.stringify(u));
    else localStorage.removeItem(STORAGE_KEYS.USER);
  }, []);

  const logout = useCallback(async () => {
    const refresh = tokenStore.getRefresh();
    if (refresh) {
      try { await authApi.logout(refresh); } catch { /* ignore */ }
    }
    tokenStore.clear();
    persistUser(null);
  }, [persistUser]);

  // Log the user out if a refresh attempt ultimately fails.
  useEffect(() => {
    setAuthFailureHandler(() => persistUser(null));
  }, [persistUser]);

  // On boot, validate the stored session against the API.
  useEffect(() => {
    let active = true;
    (async () => {
      if (tokenStore.getAccess()) {
        try {
          const me = await authApi.me();
          if (active) persistUser(me);
        } catch {
          if (active) { tokenStore.clear(); persistUser(null); }
        }
      }
      if (active) setLoading(false);
    })();
    return () => { active = false; };
  }, [persistUser]);

  const login = useCallback(async (email, password) => {
    const data = await authApi.login({ email, password });
    tokenStore.set(data.accessToken, data.refreshToken);
    persistUser(data.user);
    return data.user;
  }, [persistUser]);

  const register = useCallback(async (payload) => {
    const data = await authApi.register({ ...payload, role: 'Candidate' });
    tokenStore.set(data.accessToken, data.refreshToken);
    persistUser(data.user);
    return data.user;
  }, [persistUser]);

  const value = useMemo(() => ({
    user,
    loading,
    isAuthenticated: !!user,
    hasRole: (role) => !!user?.roles?.includes(role),
    login,
    register,
    logout,
    refreshUser: async () => persistUser(await authApi.me()),
  }), [user, loading, login, register, logout, persistUser]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// eslint-disable-next-line react-refresh/only-export-components
export const useAuth = () => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider');
  return ctx;
};
