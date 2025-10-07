import { useAuth } from '../auth/AuthContext';
import { useMockAuth0 } from '../auth/MockAuth0Provider';
import { useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { MessageSquare, Shield, Lock } from 'lucide-react';

const IS_DEMO_MODE = import.meta.env.VITE_DEMO_MODE === 'true';

export function Login() {
  // Conditionally use the appropriate auth hook based on mode
  const auth = IS_DEMO_MODE ? useMockAuth0() : useAuth();

  // Extract login, isAuthenticated, isLoading from the appropriate auth provider
  const { login, isAuthenticated, isLoading } = IS_DEMO_MODE
    ? { login: auth.loginWithRedirect, isAuthenticated: auth.isAuthenticated, isLoading: auth.isLoading }
    : { login: auth.login, isAuthenticated: auth.isAuthenticated, isLoading: auth.isLoading };

  const navigate = useNavigate();
  const location = useLocation();

  const from = (location.state as any)?.from?.pathname || '/dashboard';

  useEffect(() => {
    if (isAuthenticated) {
      navigate(from, { replace: true });
    }
  }, [isAuthenticated, navigate, from]);

  const handleLogin = () => {
    if (IS_DEMO_MODE) {
      (auth.loginWithRedirect as any)({
        appState: { returnTo: from }
      });
    } else {
      login();
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-gray-50">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-primary-50 via-blue-50 to-indigo-100 flex items-center justify-center px-4">
      <div className="max-w-md w-full">
        {/* Logo and Branding */}
        <div className="text-center mb-8">
          <div className="flex justify-center mb-4">
            <div className="w-20 h-20 bg-gradient-to-br from-primary-600 to-primary-700 rounded-2xl flex items-center justify-center shadow-2xl transform hover:scale-105 transition-transform">
              <MessageSquare className="w-12 h-12 text-white" />
            </div>
          </div>
          <h1 className="text-5xl font-bold text-gray-900 mb-2">ReviewHub</h1>
          <p className="text-lg text-gray-600">
            Enterprise Review Management Platform
          </p>
        </div>

        {/* Login Card */}
        <div className="bg-white rounded-2xl shadow-2xl p-8 border border-gray-100">
          <h2 className="text-3xl font-bold text-gray-900 mb-2">Welcome</h2>
          <p className="text-gray-600 mb-8">
            Sign in with your authorized account
          </p>

          <button
            onClick={handleLogin}
            className="w-full btn-primary py-4 text-lg font-semibold shadow-lg hover:shadow-xl transform hover:-translate-y-0.5 transition-all"
          >
            Sign In Securely
          </button>

          {/* Security Features */}
          <div className="mt-8 pt-8 border-t border-gray-200">
            <p className="text-sm font-semibold text-gray-900 mb-4 flex items-center gap-2">
              <Shield className="w-5 h-5 text-primary-600" />
              Secure Access Features
            </p>
            <ul className="space-y-3">
              <li className="flex items-start gap-3">
                <div className="w-5 h-5 bg-green-100 rounded-full flex items-center justify-center mt-0.5">
                  <svg className="w-3 h-3 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="3" d="M5 13l4 4L19 7"></path>
                  </svg>
                </div>
                <span className="text-sm text-gray-700">
                  <span className="font-medium">Multi-Factor Authentication</span> - Enhanced security for your account
                </span>
              </li>
              <li className="flex items-start gap-3">
                <div className="w-5 h-5 bg-green-100 rounded-full flex items-center justify-center mt-0.5">
                  <svg className="w-3 h-3 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="3" d="M5 13l4 4L19 7"></path>
                  </svg>
                </div>
                <span className="text-sm text-gray-700">
                  <span className="font-medium">Encrypted Data Transfer</span> - All data is encrypted in transit
                </span>
              </li>
              <li className="flex items-start gap-3">
                <div className="w-5 h-5 bg-green-100 rounded-full flex items-center justify-center mt-0.5">
                  <svg className="w-3 h-3 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="3" d="M5 13l4 4L19 7"></path>
                  </svg>
                </div>
                <span className="text-sm text-gray-700">
                  <span className="font-medium">Invitation-Only Access</span> - Accounts are created by invitation
                </span>
              </li>
              <li className="flex items-start gap-3">
                <div className="w-5 h-5 bg-green-100 rounded-full flex items-center justify-center mt-0.5">
                  <svg className="w-3 h-3 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="3" d="M5 13l4 4L19 7"></path>
                  </svg>
                </div>
                <span className="text-sm text-gray-700">
                  <span className="font-medium">Role-Based Permissions</span> - Granular access control
                </span>
              </li>
            </ul>
          </div>

          {/* Access Note */}
          <div className="mt-6 p-4 bg-blue-50 border border-blue-200 rounded-lg">
            <div className="flex gap-3">
              <Lock className="w-5 h-5 text-blue-600 flex-shrink-0 mt-0.5" />
              <div>
                <p className="text-sm font-medium text-blue-900">Need Access?</p>
                <p className="text-sm text-blue-700 mt-1">
                  Contact your business administrator to receive an invitation link. New accounts must be created through team invitations.
                </p>
              </div>
            </div>
          </div>
        </div>

        {/* Footer */}
        <p className="text-center text-sm text-gray-600 mt-8">
          By signing in, you agree to our{' '}
          <a href="#" className="text-primary-600 hover:text-primary-700 font-medium">
            Terms of Service
          </a>{' '}
          and{' '}
          <a href="#" className="text-primary-600 hover:text-primary-700 font-medium">
            Privacy Policy
          </a>
        </p>

        <p className="text-center text-xs text-gray-500 mt-4">
          Protected by enterprise-grade security · Auth0 powered
        </p>
      </div>
    </div>
  );
}
