import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { AuthProvider, AuthCallback } from "./auth/AuthContext";
import { MockAuth0Provider } from "./auth/MockAuth0Provider";
import { LocationProvider } from "./contexts/LocationContext";
import { ProtectedRoute } from "./components/ProtectedRoute";
import { Navigation } from "./components/Navigation";
import { Login } from "./pages/Login";
import { RegisterComplete } from "./components/RegisterComplete";
import { AcceptInvitation } from "./pages/AcceptInvitation";
import { Dashboard } from "./pages/Dashboard";
import { Reviews } from "./pages/Reviews";
import { Integrations } from "./pages/Integrations";
import { Analytics } from "./pages/Analytics";
import { POSAutomation } from "./pages/POSAutomation";
import { Competitors } from "./pages/Competitors";
import { Settings } from "./pages/Settings";
import { Notifications } from "./pages/Notifications";
import { AIInsights } from "./pages/AIInsights";
import { Locations } from "./pages/Locations";
import { LocationComparison } from "./pages/LocationComparison";

const IS_DEMO_MODE = import.meta.env.VITE_DEMO_MODE === "true";

// Wrapper component for protected routes with LocationProvider
function ProtectedRouteWithLocation({ children }: { children: React.ReactNode }) {
  return (
    <ProtectedRoute>
      <LocationProvider>
        {children}
      </LocationProvider>
    </ProtectedRoute>
  );
}

function App() {
  // Use MockAuth0Provider in demo mode, real AuthProvider in production
  const Provider = IS_DEMO_MODE ? MockAuth0Provider : AuthProvider;

  return (
    <BrowserRouter basename="/">
      <Provider>
        <div className="min-h-screen bg-gray-50">
          <Routes>
            <Route path="/auth/callback" element={<AuthCallback />} />
            <Route path="/login" element={<Login />} />
            <Route path="/accept-invitation" element={<AcceptInvitation />} />
              <Route
                path="/register-complete"
                element={
                  <ProtectedRouteWithLocation>
                    <RegisterComplete />
                  </ProtectedRouteWithLocation>
                }
              />
              <Route
                path="/"
                element={
                  <ProtectedRouteWithLocation>
                    <Navigate to="/dashboard" replace />
                  </ProtectedRouteWithLocation>
                }
              />
              <Route
                path="/dashboard"
                element={
                  <ProtectedRouteWithLocation>
                    <>
                      <Navigation />
                      <div className="pt-16 lg:pl-64">
                        <Dashboard />
                      </div>
                    </>
                  </ProtectedRouteWithLocation>
                }
              />
              <Route
                path="/reviews"
                element={
                  <ProtectedRouteWithLocation>
                    <>
                      <Navigation />
                      <div className="pt-16 lg:pl-64">
                        <Reviews />
                      </div>
                    </>
                  </ProtectedRouteWithLocation>
                }
              />
              <Route
                path="/integrations"
                element={
                  <ProtectedRouteWithLocation>
                    <>
                      <Navigation />
                      <div className="pt-16 lg:pl-64">
                        <Integrations />
                      </div>
                    </>
                  </ProtectedRouteWithLocation>
                }
              />
              <Route
                path="/analytics"
                element={
                  <ProtectedRouteWithLocation>
                    <>
                      <Navigation />
                      <div className="pt-16 lg:pl-64">
                        <Analytics />
                      </div>
                    </>
                  </ProtectedRouteWithLocation>
                }
              />
              <Route
                path="/pos-automation"
                element={
                  <ProtectedRouteWithLocation>
                    <>
                      <Navigation />
                      <div className="pt-16 lg:pl-64">
                        <POSAutomation />
                      </div>
                    </>
                  </ProtectedRouteWithLocation>
                }
              />
              <Route
                path="/competitors"
                element={
                  <ProtectedRouteWithLocation>
                    <>
                      <Navigation />
                      <div className="pt-16 lg:pl-64">
                        <Competitors />
                      </div>
                    </>
                  </ProtectedRouteWithLocation>
                }
              />
              <Route
                path="/ai-insights"
                element={
                  <ProtectedRouteWithLocation>
                    <>
                      <Navigation />
                      <div className="pt-16 lg:pl-64">
                        <AIInsights />
                      </div>
                    </>
                  </ProtectedRouteWithLocation>
                }
              />
              <Route
                path="/settings"
                element={
                  <ProtectedRouteWithLocation>
                    <>
                      <Navigation />
                      <div className="pt-16 lg:pl-64">
                        <Settings />
                      </div>
                    </>
                  </ProtectedRouteWithLocation>
                }
              />
              <Route
                path="/notifications"
                element={
                  <ProtectedRouteWithLocation>
                    <>
                      <Navigation />
                      <div className="pt-16 lg:pl-64">
                        <Notifications />
                      </div>
                    </>
                  </ProtectedRouteWithLocation>
                }
              />
              <Route
                path="/locations"
                element={
                  <ProtectedRouteWithLocation>
                    <>
                      <Navigation />
                      <div className="pt-16 lg:pl-64">
                        <Locations />
                      </div>
                    </>
                  </ProtectedRouteWithLocation>
                }
              />
              <Route
                path="/location-comparison"
                element={
                  <ProtectedRouteWithLocation>
                    <>
                      <Navigation />
                      <div className="pt-16 lg:pl-64">
                        <LocationComparison />
                      </div>
                    </>
                  </ProtectedRouteWithLocation>
                }
              />
            </Routes>
          </div>
      </Provider>
    </BrowserRouter>
  );
}

export default App;
