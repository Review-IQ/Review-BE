import { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import { api } from '../services/api';

export interface Location {
  id: number;
  name: string;
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
  phoneNumber?: string;
  email?: string;
  latitude?: number;
  longitude?: number;
  isActive: boolean;
  locationGroup?: {
    id: number;
    name: string;
    groupType?: string;
  };
  manager?: {
    id: number;
    fullName: string;
    email: string;
  };
}

interface LocationContextType {
  selectedLocationId: number | null;
  setSelectedLocationId: (id: number | null) => void;
  locations: Location[];
  isLoading: boolean;
  refreshLocations: () => Promise<void>;
}

const LocationContext = createContext<LocationContextType | undefined>(undefined);

export function LocationProvider({ children }: { children: ReactNode }) {
  const [selectedLocationId, setSelectedLocationIdState] = useState<number | null>(null);
  const [locations, setLocations] = useState<Location[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const fetchLocations = async () => {
    try {
      setIsLoading(true);
      console.log('[LocationContext] Fetching locations...');
      const response = await api.getLocations();
      console.log('[LocationContext] Locations fetched:', response.data);
      setLocations(response.data);

      // Restore selected location from localStorage if available
      const savedLocationId = localStorage.getItem('selectedLocationId');
      console.log('[LocationContext] Saved location ID from localStorage:', savedLocationId);
      if (savedLocationId && response.data.some((loc: Location) => loc.id === parseInt(savedLocationId))) {
        console.log('[LocationContext] Restoring location ID:', parseInt(savedLocationId));
        setSelectedLocationIdState(parseInt(savedLocationId));
      }
    } catch (error) {
      console.error('Failed to fetch locations:', error);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchLocations();
  }, []);

  const setSelectedLocationId = (id: number | null) => {
    console.log('[LocationContext] Setting selectedLocationId to:', id);
    setSelectedLocationIdState(id);
    if (id === null) {
      localStorage.removeItem('selectedLocationId');
    } else {
      localStorage.setItem('selectedLocationId', id.toString());
    }
  };

  const refreshLocations = async () => {
    await fetchLocations();
  };

  return (
    <LocationContext.Provider
      value={{
        selectedLocationId,
        setSelectedLocationId,
        locations,
        isLoading,
        refreshLocations,
      }}
    >
      {children}
    </LocationContext.Provider>
  );
}

export function useLocation() {
  const context = useContext(LocationContext);
  if (context === undefined) {
    throw new Error('useLocation must be used within a LocationProvider');
  }
  return context;
}
