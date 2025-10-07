import { MapPin } from 'lucide-react';
import { useLocation } from '../contexts/LocationContext';

export default function LocationSelector() {
  const { selectedLocationId, setSelectedLocationId, locations, isLoading } = useLocation();

  if (isLoading) {
    return (
      <div className="flex items-center gap-2 px-3 py-2 text-sm text-gray-500">
        <MapPin className="h-4 w-4 animate-pulse" />
        <span>Loading locations...</span>
      </div>
    );
  }

  if (locations.length === 0) {
    return (
      <div className="flex items-center gap-2 px-3 py-2 text-sm text-gray-500">
        <MapPin className="h-4 w-4" />
        <span className="text-xs">No locations</span>
      </div>
    );
  }

  return (
    <div className="relative inline-block">
      <div className="flex items-center gap-2">
        <MapPin className="h-4 w-4 text-gray-500" />
        <select
          value={selectedLocationId || 'all'}
          onChange={(e) => {
            const value = e.target.value;
            setSelectedLocationId(value === 'all' ? null : parseInt(value));
          }}
          className="block w-full rounded-md border-gray-300 py-2 pl-3 pr-10 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
        >
          <option value="all">All Locations ({locations.length})</option>
          {locations.map((location) => (
            <option key={location.id} value={location.id}>
              {location.name}
              {location.city && ` - ${location.city}`}
              {location.state && `, ${location.state}`}
            </option>
          ))}
        </select>
      </div>
    </div>
  );
}
