import { createContext, useContext, useState, type ReactNode } from 'react';

// Mock Auth0 context that mimics the real Auth0Context
interface MockAuth0ContextType {
  isAuthenticated: boolean;
  isLoading: boolean;
  user?: {
    sub: string;
    name: string;
    email: string;
    picture?: string;
  };
  loginWithRedirect: (options?: any) => Promise<void>;
  logout: (options?: any) => void;
  getAccessTokenSilently: () => Promise<string>;
}

const MockAuth0Context = createContext<MockAuth0ContextType | null>(null);

export const useMockAuth0 = () => {
  const context = useContext(MockAuth0Context);
  if (!context) {
    throw new Error('useMockAuth0 must be used within MockAuth0Provider');
  }
  return context;
};

interface MockAuth0ProviderProps {
  children: ReactNode;
}

export const MockAuth0Provider = ({ children }: MockAuth0ProviderProps) => {
  const [isAuthenticated, setIsAuthenticated] = useState(true); // Auto-login in demo mode
  const [isLoading, setIsLoading] = useState(false);

  const mockUser = {
    sub: 'demo|123456789',
    name: 'Demo User',
    email: 'demo@reviewhub.com',
    picture: 'https://ui-avatars.com/api/?name=Demo+User&background=0284c7&color=fff'
  };

  const loginWithRedirect = async () => {
    setIsLoading(true);
    // Simulate login delay
    await new Promise(resolve => setTimeout(resolve, 500));
    setIsAuthenticated(true);
    setIsLoading(false);
  };

  const logout = () => {
    setIsAuthenticated(false);
    window.location.href = '/login';
  };

  const getAccessTokenSilently = async () => {
    // Return a fake token for demo mode
    return 'demo-token-12345';
  };

  const value: MockAuth0ContextType = {
    isAuthenticated,
    isLoading,
    user: isAuthenticated ? mockUser : undefined,
    loginWithRedirect,
    logout,
    getAccessTokenSilently
  };

  return (
    <MockAuth0Context.Provider value={value}>
      {children}
    </MockAuth0Context.Provider>
  );
};
