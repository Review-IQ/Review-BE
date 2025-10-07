import { Navigate, useLocation } from 'react-router-dom';
import { type ReactNode, useEffect, useState } from 'react';
import { api } from '../services/api';
import { useAuth } from '../auth/AuthContext';
import { useMockAuth0 } from '../auth/MockAuth0Provider';

const IS_DEMO_MODE = import.meta.env.VITE_DEMO_MODE === 'true';

interface ProtectedRouteProps {
  children: ReactNode;
}

export const ProtectedRoute = ({ children }: ProtectedRouteProps) => {
  // Use the appropriate auth hook based on demo mode
  const auth = IS_DEMO_MODE ? useMockAuth0() : useAuth();
  const { isAuthenticated, isLoading } = auth;

  const location = useLocation();
  const [checkingUser, setCheckingUser] = useState(true);
  const [needsRegistration, setNeedsRegistration] = useState(false);
  const [isInactive, setIsInactive] = useState(false);

  useEffect(() => {
    const checkUserInSystem = async () => {
      if (isAuthenticated && !isLoading) {
        try {
          await api.getCurrentUser();
          // User exists and is active in our system
          setNeedsRegistration(false);
          setIsInactive(false);
        } catch (error: any) {
          if (error.response?.status === 404) {
            // User not found in our system - needs to complete registration
            setNeedsRegistration(true);
          } else if (error.response?.status === 403) {
            // User account is inactive
            setIsInactive(true);
          }
        } finally {
          setCheckingUser(false);
        }
      } else if (!isLoading) {
        setCheckingUser(false);
      }
    };

    checkUserInSystem();
  }, [isAuthenticated, isLoading]);

  if (isLoading || checkingUser) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  if (!isAuthenticated) {
    // Redirect to login page, but save the attempted location
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  if (isInactive) {
    // User account is inactive
    return (
      <div className="flex items-center justify-center min-h-screen bg-gray-50">
        <div className="max-w-md w-full bg-white rounded-lg shadow-lg p-8 text-center">
          <div className="w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <svg className="w-8 h-8 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path>
            </svg>
          </div>
          <h2 className="text-2xl font-bold text-gray-900 mb-2">Account Inactive</h2>
          <p className="text-gray-600 mb-6">
            Your account has been deactivated. Please contact support for assistance.
          </p>
          <a href="mailto:support@reviewhub.com" className="btn-primary inline-block">
            Contact Support
          </a>
        </div>
      </div>
    );
  }

  if (needsRegistration && location.pathname !== '/register-complete') {
    // User needs to complete registration in our system
    return <Navigate to="/register-complete" replace />;
  }

  return <>{children}</>;
};
