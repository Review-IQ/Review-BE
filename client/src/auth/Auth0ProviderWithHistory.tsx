import { Auth0Provider } from '@auth0/auth0-react';
import { useNavigate } from 'react-router-dom';
import { type ReactNode } from 'react';

interface Auth0ProviderWithHistoryProps {
  children: ReactNode;
}

function Auth0TokenSetup({ children }: { children: ReactNode }) {
  return <>{children}</>;
}

export const Auth0ProviderWithHistory = ({ children }: Auth0ProviderWithHistoryProps) => {
  const navigate = useNavigate();

  const domain = import.meta.env.VITE_AUTH0_DOMAIN || 'your-domain.auth0.com';
  const clientId = import.meta.env.VITE_AUTH0_CLIENT_ID || 'your-client-id';
  const audience = import.meta.env.VITE_AUTH0_AUDIENCE || 'https://api.reviewhub.com';
  const redirectUri = window.location.origin;

  const onRedirectCallback = (appState: any) => {
    navigate(appState?.returnTo || window.location.pathname);
  };

  return (
    <Auth0Provider
      domain={domain}
      clientId={clientId}
      authorizationParams={{
        redirect_uri: redirectUri,
        audience: audience,
        scope: 'openid profile email'
      }}
      onRedirectCallback={onRedirectCallback}
      useRefreshTokens={true}
      cacheLocation="localstorage"
    >
      <Auth0TokenSetup>{children}</Auth0TokenSetup>
    </Auth0Provider>
  );
};
