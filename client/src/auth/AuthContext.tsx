import { createContext, useContext, useState, useEffect, type ReactNode } from 'react';

interface User {
  id: number;
  email: string;
  fullName: string;
  companyName?: string;
  phoneNumber?: string;
  subscriptionPlan: string;
}

interface AuthContextType {
  isAuthenticated: boolean;
  isLoading: boolean;
  user: User | null;
  accessToken: string | null;
  login: () => void;
  logout: () => void;
  getAccessToken: () => string | null;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [user, setUser] = useState<User | null>(null);
  const [accessToken, setAccessToken] = useState<string | null>(null);

  useEffect(() => {
    // Check if we have a token in localStorage
    const storedToken = localStorage.getItem('access_token');
    const tokenExpiry = localStorage.getItem('token_expiry');

    if (storedToken && tokenExpiry) {
      const expiryTime = parseInt(tokenExpiry, 10);
      if (Date.now() < expiryTime) {
        setAccessToken(storedToken);
        setIsAuthenticated(true); // Set authenticated if token exists and is valid
        fetchUser(storedToken);
      } else {
        // Token expired
        localStorage.removeItem('access_token');
        localStorage.removeItem('token_expiry');
        setIsLoading(false);
      }
    } else {
      setIsLoading(false);
    }
  }, []);

  const fetchUser = async (token: string) => {
    try {
      const response = await fetch(`${API_URL}/auth/me`, {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      if (response.ok) {
        const userData = await response.json();
        setUser(userData);
      } else if (response.status === 404) {
        // User not in system but token is valid - ProtectedRoute will handle redirect
        const data = await response.json();
        console.log('User needs registration:', data);
        // Keep isAuthenticated = true so ProtectedRoute can handle it
      } else {
        // Token invalid (401, 403, etc)
        localStorage.removeItem('access_token');
        localStorage.removeItem('token_expiry');
        setAccessToken(null);
        setIsAuthenticated(false);
      }
    } catch (error) {
      console.error('Error fetching user:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const login = () => {
    // Redirect to backend login endpoint
    window.location.href = `${API_URL}/auth/login?returnUrl=${encodeURIComponent(window.location.pathname)}`;
  };

  const logout = () => {
    localStorage.removeItem('access_token');
    localStorage.removeItem('token_expiry');
    setAccessToken(null);
    setUser(null);
    setIsAuthenticated(false);

    // Redirect to backend logout endpoint
    window.location.href = `${API_URL}/auth/logout`;
  };

  const getAccessToken = () => {
    return accessToken;
  };

  return (
    <AuthContext.Provider
      value={{
        isAuthenticated,
        isLoading,
        user,
        accessToken,
        login,
        logout,
        getAccessToken,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}

// Helper component to handle the Auth0 callback
export function AuthCallback() {
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const accessToken = params.get('access_token');
    const idToken = params.get('id_token');
    const expiresIn = params.get('expires_in');
    let returnUrl = params.get('return_url') || '/dashboard';
    const errorParam = params.get('error');

    // Never return to login page after auth - always go to dashboard
    if (returnUrl === '/login' || returnUrl === '/') {
      returnUrl = '/dashboard';
    }

    if (errorParam) {
      setError(errorParam);
      setTimeout(() => {
        window.location.href = '/login';
      }, 3000);
      return;
    }

    if (accessToken && expiresIn) {
      // Store tokens
      const expiryTime = Date.now() + (parseInt(expiresIn, 10) * 1000);
      localStorage.setItem('access_token', accessToken);
      localStorage.setItem('token_expiry', expiryTime.toString());
      if (idToken) {
        localStorage.setItem('id_token', idToken);
      }

      // Redirect to return URL
      window.location.href = returnUrl;
    } else {
      setError('No access token received');
      setTimeout(() => {
        window.location.href = '/login';
      }, 3000);
    }
  }, []);

  if (error) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-gray-50">
        <div className="text-center">
          <h2 className="text-2xl font-bold text-red-600 mb-2">Authentication Error</h2>
          <p className="text-gray-600">{error}</p>
          <p className="text-sm text-gray-500 mt-2">Redirecting to login...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="flex items-center justify-center min-h-screen bg-gray-50">
      <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
    </div>
  );
}
