import { useState, useEffect } from 'react';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { MapPin, TrendingUp, Star, MessageSquare, ThumbsUp } from 'lucide-react';
import { api } from '../services/api';
import { useLocation } from '../contexts/LocationContext';

interface LocationComparison {
  locationId: number;
  locationName: string;
  totalReviews: number;
  averageRating: number;
  positiveSentiment: number;
  neutralSentiment: number;
  negativeSentiment: number;
  responseRate: number;
  recentReviews: number;
}

export function LocationComparison() {
  const { locations } = useLocation();
  const [selectedLocationIds, setSelectedLocationIds] = useState<number[]>([]);
  const [comparisonData, setComparisonData] = useState<LocationComparison[]>([]);
  const [loading, setLoading] = useState(false);

  const handleLocationToggle = (locationId: number) => {
    setSelectedLocationIds(prev => {
      if (prev.includes(locationId)) {
        return prev.filter(id => id !== locationId);
      } else {
        if (prev.length >= 5) {
          alert('You can compare up to 5 locations at a time');
          return prev;
        }
        return [...prev, locationId];
      }
    });
  };

  const loadComparison = async () => {
    if (selectedLocationIds.length === 0) {
      setComparisonData([]);
      return;
    }

    try {
      setLoading(true);
      const response = await api.compareLocations(selectedLocationIds);
      setComparisonData(response.data);
    } catch (error) {
      console.error('Error loading comparison:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadComparison();
  }, [selectedLocationIds]);

  const chartData = comparisonData.map(loc => ({
    name: loc.locationName,
    'Avg Rating': loc.averageRating,
    'Reviews': loc.totalReviews / 10, // Scale down for chart
    'Response Rate': loc.responseRate,
  }));

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">Location Comparison</h1>
        <p className="mt-2 text-sm text-gray-700">
          Compare performance metrics across your locations
        </p>
      </div>

      {/* Location Selection */}
      <div className="bg-white shadow rounded-lg p-6 mb-8">
        <h2 className="text-lg font-medium text-gray-900 mb-4">
          Select Locations to Compare (up to 5)
        </h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {locations.map(location => (
            <label
              key={location.id}
              className={`flex items-center p-4 border-2 rounded-lg cursor-pointer transition-colors ${
                selectedLocationIds.includes(location.id)
                  ? 'border-blue-500 bg-blue-50'
                  : 'border-gray-200 hover:border-gray-300'
              }`}
            >
              <input
                type="checkbox"
                checked={selectedLocationIds.includes(location.id)}
                onChange={() => handleLocationToggle(location.id)}
                className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
              />
              <div className="ml-3 flex-1">
                <div className="flex items-center">
                  <MapPin className="h-4 w-4 text-gray-400 mr-2" />
                  <span className="text-sm font-medium text-gray-900">
                    {location.name}
                  </span>
                </div>
                {location.city && (
                  <p className="text-xs text-gray-500 mt-1">
                    {location.city}, {location.state}
                  </p>
                )}
              </div>
            </label>
          ))}
        </div>
      </div>

      {/* Comparison Results */}
      {selectedLocationIds.length === 0 ? (
        <div className="bg-white shadow rounded-lg p-12 text-center">
          <MapPin className="h-12 w-12 text-gray-400 mx-auto mb-4" />
          <h3 className="text-lg font-medium text-gray-900 mb-2">
            No Locations Selected
          </h3>
          <p className="text-sm text-gray-500">
            Select at least one location above to start comparing performance metrics
          </p>
        </div>
      ) : loading ? (
        <div className="bg-white shadow rounded-lg p-12 text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
          <p className="text-sm text-gray-500">Loading comparison data...</p>
        </div>
      ) : (
        <>
          {/* Summary Cards */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
            {comparisonData.map(location => (
              <div key={location.locationId} className="bg-white shadow rounded-lg p-6">
                <div className="flex items-center justify-between mb-4">
                  <h3 className="text-sm font-medium text-gray-900 truncate">
                    {location.locationName}
                  </h3>
                  <MapPin className="h-5 w-5 text-blue-600 flex-shrink-0" />
                </div>

                <div className="space-y-3">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center text-sm text-gray-500">
                      <Star className="h-4 w-4 mr-1" />
                      Rating
                    </div>
                    <span className="text-lg font-semibold text-gray-900">
                      {location.averageRating.toFixed(1)}
                    </span>
                  </div>

                  <div className="flex items-center justify-between">
                    <div className="flex items-center text-sm text-gray-500">
                      <MessageSquare className="h-4 w-4 mr-1" />
                      Reviews
                    </div>
                    <span className="text-lg font-semibold text-gray-900">
                      {location.totalReviews}
                    </span>
                  </div>

                  <div className="flex items-center justify-between">
                    <div className="flex items-center text-sm text-gray-500">
                      <ThumbsUp className="h-4 w-4 mr-1" />
                      Positive
                    </div>
                    <span className="text-lg font-semibold text-green-600">
                      {location.positiveSentiment.toFixed(0)}%
                    </span>
                  </div>

                  <div className="flex items-center justify-between">
                    <div className="flex items-center text-sm text-gray-500">
                      <TrendingUp className="h-4 w-4 mr-1" />
                      Response
                    </div>
                    <span className="text-lg font-semibold text-blue-600">
                      {location.responseRate.toFixed(0)}%
                    </span>
                  </div>
                </div>
              </div>
            ))}
          </div>

          {/* Charts */}
          <div className="bg-white shadow rounded-lg p-6 mb-8">
            <h2 className="text-lg font-medium text-gray-900 mb-6">
              Performance Comparison
            </h2>
            <ResponsiveContainer width="100%" height={400}>
              <BarChart data={chartData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="name" />
                <YAxis />
                <Tooltip />
                <Legend />
                <Bar dataKey="Avg Rating" fill="#3b82f6" />
                <Bar dataKey="Reviews" fill="#10b981" />
                <Bar dataKey="Response Rate" fill="#f59e0b" />
              </BarChart>
            </ResponsiveContainer>
          </div>

          {/* Detailed Table */}
          <div className="bg-white shadow overflow-hidden sm:rounded-lg">
            <div className="px-4 py-5 sm:px-6 border-b border-gray-200">
              <h3 className="text-lg leading-6 font-medium text-gray-900">
                Detailed Metrics
              </h3>
            </div>
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Location
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Total Reviews
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Avg Rating
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Positive %
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Neutral %
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Negative %
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Response Rate
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Recent (30d)
                    </th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {comparisonData.map(location => (
                    <tr key={location.locationId}>
                      <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                        {location.locationName}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {location.totalReviews}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        <div className="flex items-center">
                          <Star className="h-4 w-4 text-yellow-400 mr-1" />
                          {location.averageRating.toFixed(2)}
                        </div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-green-600">
                        {location.positiveSentiment.toFixed(1)}%
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {location.neutralSentiment.toFixed(1)}%
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-red-600">
                        {location.negativeSentiment.toFixed(1)}%
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-blue-600">
                        {location.responseRate.toFixed(1)}%
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {location.recentReviews}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
