import {
  createContext,
  useContext,
  useState,
  useCallback,
  useEffect,
  type ReactNode,
} from 'react';
import type { AuthResponse } from '../types/auth';

interface AuthContextType {
  token: string | null;
  userId: string | null;
  isAuthenticated: boolean;
  login: (data: AuthResponse) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | null>(null);

const STORAGE_KEYS = {
  token: 'auth_token',
  userId: 'auth_user_id',
  expiresAt: 'auth_expires_at',
} as const;

function isTokenExpired(): boolean {
  const expiresAt = localStorage.getItem(STORAGE_KEYS.expiresAt);
  if (!expiresAt) return true;
  return new Date(expiresAt).getTime() < Date.now();
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => {
    return localStorage.getItem(STORAGE_KEYS.token);
  });

  const [userId, setUserId] = useState<string | null>(() => {
    return localStorage.getItem(STORAGE_KEYS.userId);
  });

  const isAuthenticated = token !== null && !isTokenExpired();

  const login = useCallback((data: AuthResponse) => {
    localStorage.setItem(STORAGE_KEYS.token, data.token);
    localStorage.setItem(STORAGE_KEYS.userId, data.userId);
    localStorage.setItem(STORAGE_KEYS.expiresAt, data.expiresAt);
    setToken(data.token);
    setUserId(data.userId);
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem(STORAGE_KEYS.token);
    localStorage.removeItem(STORAGE_KEYS.userId);
    localStorage.removeItem(STORAGE_KEYS.expiresAt);
    setToken(null);
    setUserId(null);
  }, []);

  // Автоматический logout при истечении токена
  useEffect(() => {
    if (token && isTokenExpired()) {
      logout();
    }
  }, [token, logout]);

  return (
    <AuthContext.Provider value={{ token, userId, isAuthenticated, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextType {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return ctx;
}
