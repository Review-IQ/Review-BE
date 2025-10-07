import { useState, useEffect } from 'react';
import { MapPin, Plus, Edit2, Trash2, Building2, Users, FolderTree } from 'lucide-react';
import { api } from '../services/api';
import { useLocation as useLocationContext } from '../contexts/LocationContext';
import { LocationModal } from '../components/LocationModal';
import type { Location as LocationType } from '../contexts/LocationContext';

interface Location {
  id: number;
  name: string;
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  phoneNumber?: string;
  email?: string;
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

interface LocationGroup {
  id: number;
  name: string;
  groupType?: string;
  level: number;
  parentGroupId?: number;
  childGroups: LocationGroup[];
  locations: Location[];
}

export function Locations() {
  const { locations, refreshLocations } = useLocationContext();
  const [locationGroups, setLocationGroups] = useState<LocationGroup[]>([]);
  const [loading, setLoading] = useState(true);
  const [showLocationModal, setShowLocationModal] = useState(false);
  const [editingLocation, setEditingLocation] = useState<LocationType | null>(null);
  const [organizationId] = useState(1); // TODO: Get from user context

  useEffect(() => {
    loadLocationGroups();
  }, []);

  const loadLocationGroups = async () => {
    try {
      setLoading(true);
      const response = await api.getLocationGroups();
      setLocationGroups(response.data);
    } catch (error) {
      console.error('Error loading location groups:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleAddLocation = () => {
    setEditingLocation(null);
    setShowLocationModal(true);
  };

  const handleEditLocation = (location: LocationType) => {
    setEditingLocation(location);
    setShowLocationModal(true);
  };

  const handleDeleteLocation = async (locationId: number) => {
    if (!confirm('Are you sure you want to delete this location?')) return;

    try {
      await api.deleteLocation(locationId);
      await refreshLocations();
    } catch (error) {
      console.error('Error deleting location:', error);
      alert('Failed to delete location');
    }
  };

  const handleLocationSuccess = async () => {
    await refreshLocations();
    await loadLocationGroups();
  };

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">Locations</h1>
        <p className="mt-2 text-sm text-gray-700">
          Manage your business locations and organizational hierarchy
        </p>
      </div>

      {/* Location Modal */}
      <LocationModal
        isOpen={showLocationModal}
        onClose={() => setShowLocationModal(false)}
        onSuccess={handleLocationSuccess}
        location={editingLocation}
        organizationId={organizationId}
      />

      {/* Action Buttons */}
      <div className="mb-6 flex gap-3">
        <button
          onClick={handleAddLocation}
          className="inline-flex items-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700"
        >
          <Plus className="h-4 w-4 mr-2" />
          Add Location
        </button>
        <button
          onClick={() => alert('Add group modal - Coming soon!')}
          className="inline-flex items-center px-4 py-2 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50"
        >
          <FolderTree className="h-4 w-4 mr-2" />
          Add Group
        </button>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 gap-5 sm:grid-cols-3 mb-8">
        <div className="bg-white overflow-hidden shadow rounded-lg">
          <div className="p-5">
            <div className="flex items-center">
              <div className="flex-shrink-0">
                <MapPin className="h-6 w-6 text-blue-600" />
              </div>
              <div className="ml-5 w-0 flex-1">
                <dl>
                  <dt className="text-sm font-medium text-gray-500 truncate">
                    Total Locations
                  </dt>
                  <dd className="text-2xl font-semibold text-gray-900">
                    {locations.length}
                  </dd>
                </dl>
              </div>
            </div>
          </div>
        </div>

        <div className="bg-white overflow-hidden shadow rounded-lg">
          <div className="p-5">
            <div className="flex items-center">
              <div className="flex-shrink-0">
                <FolderTree className="h-6 w-6 text-green-600" />
              </div>
              <div className="ml-5 w-0 flex-1">
                <dl>
                  <dt className="text-sm font-medium text-gray-500 truncate">
                    Location Groups
                  </dt>
                  <dd className="text-2xl font-semibold text-gray-900">
                    {locationGroups.length}
                  </dd>
                </dl>
              </div>
            </div>
          </div>
        </div>

        <div className="bg-white overflow-hidden shadow rounded-lg">
          <div className="p-5">
            <div className="flex items-center">
              <div className="flex-shrink-0">
                <Building2 className="h-6 w-6 text-purple-600" />
              </div>
              <div className="ml-5 w-0 flex-1">
                <dl>
                  <dt className="text-sm font-medium text-gray-500 truncate">
                    Active Locations
                  </dt>
                  <dd className="text-2xl font-semibold text-gray-900">
                    {locations.filter(l => l.isActive).length}
                  </dd>
                </dl>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Locations List */}
      <div className="bg-white shadow overflow-hidden sm:rounded-md">
        <div className="px-4 py-5 sm:px-6 border-b border-gray-200">
          <h3 className="text-lg leading-6 font-medium text-gray-900">
            All Locations
          </h3>
        </div>
        <ul className="divide-y divide-gray-200">
          {loading ? (
            <li className="px-4 py-12 text-center text-gray-500">
              Loading locations...
            </li>
          ) : locations.length === 0 ? (
            <li className="px-4 py-12 text-center text-gray-500">
              No locations found. Add your first location to get started.
            </li>
          ) : (
            locations.map((location) => (
              <li key={location.id} className="px-4 py-4 sm:px-6 hover:bg-gray-50">
                <div className="flex items-center justify-between">
                  <div className="flex items-center min-w-0 flex-1">
                    <div className="flex-shrink-0">
                      <MapPin className="h-8 w-8 text-blue-600" />
                    </div>
                    <div className="ml-4 flex-1">
                      <div className="flex items-center">
                        <p className="text-sm font-medium text-blue-600 truncate">
                          {location.name}
                        </p>
                        {location.locationGroup && (
                          <span className="ml-2 inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-800">
                            <FolderTree className="h-3 w-3 mr-1" />
                            {location.locationGroup.name}
                          </span>
                        )}
                      </div>
                      <div className="mt-1 flex items-center text-sm text-gray-500">
                        <span>
                          {[location.address, location.city, location.state, location.zipCode]
                            .filter(Boolean)
                            .join(', ') || 'No address'}
                        </span>
                      </div>
                      {location.manager && (
                        <div className="mt-1 flex items-center text-xs text-gray-500">
                          <Users className="h-3 w-3 mr-1" />
                          Manager: {location.manager.fullName}
                        </div>
                      )}
                    </div>
                  </div>
                  <div className="ml-4 flex-shrink-0 flex gap-2">
                    <button
                      onClick={() => handleEditLocation(location)}
                      className="p-2 text-gray-400 hover:text-blue-600 transition-colors"
                      title="Edit location"
                    >
                      <Edit2 className="h-5 w-5" />
                    </button>
                    <button
                      onClick={() => handleDeleteLocation(location.id)}
                      className="p-2 text-gray-400 hover:text-red-600 transition-colors"
                      title="Delete location"
                    >
                      <Trash2 className="h-5 w-5" />
                    </button>
                  </div>
                </div>
              </li>
            ))
          )}
        </ul>
      </div>

      {/* Location Groups */}
      {locationGroups.length > 0 && (
        <div className="mt-8 bg-white shadow overflow-hidden sm:rounded-md">
          <div className="px-4 py-5 sm:px-6 border-b border-gray-200">
            <h3 className="text-lg leading-6 font-medium text-gray-900">
              Location Groups
            </h3>
          </div>
          <ul className="divide-y divide-gray-200">
            {locationGroups.map((group) => (
              <li key={group.id} className="px-4 py-4 sm:px-6 hover:bg-gray-50">
                <div className="flex items-center justify-between">
                  <div className="flex items-center min-w-0 flex-1">
                    <div className="flex-shrink-0">
                      <FolderTree className="h-8 w-8 text-green-600" />
                    </div>
                    <div className="ml-4 flex-1">
                      <p className="text-sm font-medium text-gray-900 truncate">
                        {group.name}
                      </p>
                      {group.groupType && (
                        <p className="mt-1 text-sm text-gray-500">
                          Type: {group.groupType} • Level {group.level}
                        </p>
                      )}
                      <p className="mt-1 text-xs text-gray-500">
                        {group.locations.length} location{group.locations.length !== 1 ? 's' : ''}
                      </p>
                    </div>
                  </div>
                  <div className="ml-4 flex-shrink-0 flex gap-2">
                    <button className="p-2 text-gray-400 hover:text-blue-600 transition-colors">
                      <Edit2 className="h-5 w-5" />
                    </button>
                    <button className="p-2 text-gray-400 hover:text-red-600 transition-colors">
                      <Trash2 className="h-5 w-5" />
                    </button>
                  </div>
                </div>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
