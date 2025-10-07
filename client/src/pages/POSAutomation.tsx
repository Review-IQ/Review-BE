import { useState, useEffect } from 'react';
import {
  MessageSquare,
  Send,
  Users,
  CheckCircle,
  Calendar
} from 'lucide-react';
import { useLocation } from '../contexts/LocationContext';
import { api } from '../services/api';

interface Customer {
  id: number;
  name: string;
  email: string;
  phone: string;
  lastVisit: string;
  totalVisits: number;
  hasReviewed: boolean;
}

interface Campaign {
  id: number;
  name: string;
  message: string;
  status: 'draft' | 'scheduled' | 'sent' | 'active';
  scheduledFor?: string;
  recipientCount: number;
  sentCount: number;
  responseRate: number;
  createdAt: string;
}

interface MessageTemplate {
  id: number;
  name: string;
  message: string;
  category: 'review_request' | 'thank_you' | 'follow_up' | 'promotion';
}

export function POSAutomation() {
  const { selectedLocationId } = useLocation();
  const [activeTab, setActiveTab] = useState<'customers' | 'campaigns' | 'templates'>('customers');
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [campaigns, setCampaigns] = useState<Campaign[]>([]);
  const [templates, setTemplates] = useState<MessageTemplate[]>([]);
  const [selectedCustomers, setSelectedCustomers] = useState<number[]>([]);
  const [loading, setLoading] = useState(true);

  // New Campaign Modal State
  const [showNewCampaign, setShowNewCampaign] = useState(false);
  const [newCampaignName, setNewCampaignName] = useState('');
  const [newCampaignMessage, setNewCampaignMessage] = useState('');

  useEffect(() => {
    loadData();
  }, [selectedLocationId]);

  const loadData = async () => {
    try {
      setLoading(true);
      const businessId = 1; // TODO: Get from context

      // Fetch customers and campaigns from API with location filtering
      const [customersRes, campaignsRes] = await Promise.all([
        api.getCustomers(businessId, 1, 50, selectedLocationId),
        api.getCampaigns(businessId, 1, 20, selectedLocationId)
      ]);

      // Map API data to component interfaces
      const fetchedCustomers: Customer[] = customersRes.data.map((c: any) => ({
        id: c.id,
        name: c.name,
        email: c.email,
        phone: c.phone || c.phoneNumber,
        lastVisit: c.lastVisit,
        totalVisits: c.totalVisits,
        hasReviewed: false // TODO: Get from API
      }));

      const fetchedCampaigns: Campaign[] = campaignsRes.data.map((c: any) => ({
        id: c.id,
        name: c.name,
        message: c.message,
        status: c.status.toLowerCase() as any,
        scheduledFor: c.scheduledFor,
        recipientCount: c.recipientCount,
        sentCount: c.recipientCount, // TODO: Get actual sent count from API
        responseRate: c.responseRate || 0,
        createdAt: c.sentAt || c.scheduledFor || new Date().toISOString()
      }));

      const mockTemplates: MessageTemplate[] = [
        {
          id: 1,
          name: 'Review Request - Standard',
          message: 'Hi {name}! Thanks for visiting {business_name}. We\'d love to hear about your experience. Could you take a moment to leave us a review? {review_link}',
          category: 'review_request'
        },
        {
          id: 2,
          name: 'Thank You Message',
          message: 'Thank you for choosing {business_name}, {name}! We hope to see you again soon!',
          category: 'thank_you'
        },
        {
          id: 3,
          name: 'Follow-up after 3 days',
          message: 'Hi {name}, we hope you enjoyed your recent visit to {business_name}. We\'d appreciate your feedback!',
          category: 'follow_up'
        }
      ];

      setCustomers(fetchedCustomers);
      setCampaigns(fetchedCampaigns);
      setTemplates(mockTemplates);
    } catch (error) {
      console.error('Error loading data:', error);
    } finally {
      setLoading(false);
    }
  };

  const toggleCustomerSelection = (customerId: number) => {
    setSelectedCustomers(prev =>
      prev.includes(customerId)
        ? prev.filter(id => id !== customerId)
        : [...prev, customerId]
    );
  };

  const sendMessage = async () => {
    if (selectedCustomers.length === 0) {
      alert('Please select at least one customer');
      return;
    }
    // TODO: Implement SMS sending via API
    alert(`Sending message to ${selectedCustomers.length} customer(s)`);
  };

  const createCampaign = async () => {
    if (!newCampaignName || !newCampaignMessage) {
      alert('Please fill in all fields');
      return;
    }
    // TODO: Call API to create campaign
    console.log('Creating campaign:', { name: newCampaignName, message: newCampaignMessage });
    setShowNewCampaign(false);
    setNewCampaignName('');
    setNewCampaignMessage('');
    await loadData();
  };

  const getTimeSince = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const days = Math.floor((now.getTime() - date.getTime()) / 86400000);

    if (days === 0) return 'Today';
    if (days === 1) return 'Yesterday';
    return `${days} days ago`;
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center h-screen">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  return (
    <div className="px-4 sm:px-6 lg:px-8 py-8">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 flex items-center gap-3">
          <MessageSquare className="w-8 h-8 text-primary-600" />
          POS Automation
        </h1>
        <p className="mt-2 text-gray-600">
          Send automated messages to your customers and request reviews
        </p>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-8">
        <div className="bg-white rounded-lg shadow-md p-6 border border-gray-200">
          <div className="flex items-center gap-3 mb-2">
            <Users className="w-8 h-8 text-blue-600" />
          </div>
          <p className="text-sm text-gray-600">Total Customers</p>
          <p className="text-3xl font-bold text-gray-900 mt-1">{customers.length}</p>
        </div>

        <div className="bg-white rounded-lg shadow-md p-6 border border-gray-200">
          <div className="flex items-center gap-3 mb-2">
            <Send className="w-8 h-8 text-green-600" />
          </div>
          <p className="text-sm text-gray-600">Messages Sent</p>
          <p className="text-3xl font-bold text-gray-900 mt-1">
            {campaigns.reduce((sum, c) => sum + c.sentCount, 0)}
          </p>
        </div>

        <div className="bg-white rounded-lg shadow-md p-6 border border-gray-200">
          <div className="flex items-center gap-3 mb-2">
            <CheckCircle className="w-8 h-8 text-purple-600" />
          </div>
          <p className="text-sm text-gray-600">Avg Response Rate</p>
          <p className="text-3xl font-bold text-gray-900 mt-1">
            {campaigns.length > 0
              ? (campaigns.reduce((sum, c) => sum + c.responseRate, 0) / campaigns.length).toFixed(1)
              : 0}%
          </p>
        </div>

        <div className="bg-white rounded-lg shadow-md p-6 border border-gray-200">
          <div className="flex items-center gap-3 mb-2">
            <Calendar className="w-8 h-8 text-orange-600" />
          </div>
          <p className="text-sm text-gray-600">Active Campaigns</p>
          <p className="text-3xl font-bold text-gray-900 mt-1">
            {campaigns.filter(c => c.status === 'active').length}
          </p>
        </div>
      </div>

      {/* Tabs */}
      <div className="bg-white rounded-lg shadow-md border border-gray-200 mb-6">
        <div className="border-b border-gray-200">
          <div className="flex gap-4 px-6">
            <button
              onClick={() => setActiveTab('customers')}
              className={`py-4 px-2 border-b-2 font-medium text-sm transition-colors ${
                activeTab === 'customers'
                  ? 'border-primary-600 text-primary-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              Customers
            </button>
            <button
              onClick={() => setActiveTab('campaigns')}
              className={`py-4 px-2 border-b-2 font-medium text-sm transition-colors ${
                activeTab === 'campaigns'
                  ? 'border-primary-600 text-primary-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              Campaigns
            </button>
            <button
              onClick={() => setActiveTab('templates')}
              className={`py-4 px-2 border-b-2 font-medium text-sm transition-colors ${
                activeTab === 'templates'
                  ? 'border-primary-600 text-primary-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              Templates
            </button>
          </div>
        </div>

        <div className="p-6">
          {/* Customers Tab */}
          {activeTab === 'customers' && (
            <div>
              <div className="flex items-center justify-between mb-6">
                <h2 className="text-lg font-semibold text-gray-900">Customer List</h2>
                <button
                  onClick={sendMessage}
                  disabled={selectedCustomers.length === 0}
                  className="btn-primary flex items-center gap-2 disabled:opacity-50"
                >
                  <Send className="w-4 h-4" />
                  Send Message ({selectedCustomers.length})
                </button>
              </div>

              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead>
                    <tr>
                      <th className="px-6 py-3 text-left">
                        <input
                          type="checkbox"
                          onChange={(e) => {
                            if (e.target.checked) {
                              setSelectedCustomers(customers.map(c => c.id));
                            } else {
                              setSelectedCustomers([]);
                            }
                          }}
                          className="w-4 h-4 text-primary-600 border-gray-300 rounded"
                        />
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                        Name
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                        Contact
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                        Last Visit
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                        Total Visits
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                        Status
                      </th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    {customers.map((customer) => (
                      <tr key={customer.id} className="hover:bg-gray-50">
                        <td className="px-6 py-4">
                          <input
                            type="checkbox"
                            checked={selectedCustomers.includes(customer.id)}
                            onChange={() => toggleCustomerSelection(customer.id)}
                            className="w-4 h-4 text-primary-600 border-gray-300 rounded"
                          />
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <span className="text-sm font-medium text-gray-900">
                            {customer.name}
                          </span>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="text-sm text-gray-900">{customer.phone}</div>
                          <div className="text-xs text-gray-500">{customer.email}</div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                          {getTimeSince(customer.lastVisit)}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                          {customer.totalVisits}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          {customer.hasReviewed ? (
                            <span className="px-2 py-1 text-xs font-medium bg-green-100 text-green-800 rounded-full">
                              Reviewed
                            </span>
                          ) : (
                            <span className="px-2 py-1 text-xs font-medium bg-yellow-100 text-yellow-800 rounded-full">
                              Pending
                            </span>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* Campaigns Tab */}
          {activeTab === 'campaigns' && (
            <div>
              <div className="flex items-center justify-between mb-6">
                <h2 className="text-lg font-semibold text-gray-900">Message Campaigns</h2>
                <button
                  onClick={() => setShowNewCampaign(true)}
                  className="btn-primary"
                >
                  Create Campaign
                </button>
              </div>

              <div className="space-y-4">
                {campaigns.map((campaign) => (
                  <div key={campaign.id} className="border border-gray-200 rounded-lg p-6">
                    <div className="flex items-start justify-between mb-4">
                      <div>
                        <h3 className="text-lg font-semibold text-gray-900">{campaign.name}</h3>
                        <p className="text-sm text-gray-600 mt-1">{campaign.message}</p>
                      </div>
                      <span className={`px-3 py-1 text-xs font-medium rounded-full ${
                        campaign.status === 'active' ? 'bg-green-100 text-green-800' :
                        campaign.status === 'scheduled' ? 'bg-blue-100 text-blue-800' :
                        campaign.status === 'sent' ? 'bg-gray-100 text-gray-800' :
                        'bg-yellow-100 text-yellow-800'
                      }`}>
                        {campaign.status.charAt(0).toUpperCase() + campaign.status.slice(1)}
                      </span>
                    </div>

                    <div className="grid grid-cols-3 gap-4 text-sm">
                      <div>
                        <p className="text-gray-600">Recipients</p>
                        <p className="font-semibold text-gray-900">{campaign.recipientCount}</p>
                      </div>
                      <div>
                        <p className="text-gray-600">Sent</p>
                        <p className="font-semibold text-gray-900">{campaign.sentCount}</p>
                      </div>
                      <div>
                        <p className="text-gray-600">Response Rate</p>
                        <p className="font-semibold text-gray-900">{campaign.responseRate}%</p>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Templates Tab */}
          {activeTab === 'templates' && (
            <div>
              <div className="flex items-center justify-between mb-6">
                <h2 className="text-lg font-semibold text-gray-900">Message Templates</h2>
                <button className="btn-primary">
                  Create Template
                </button>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {templates.map((template) => (
                  <div key={template.id} className="border border-gray-200 rounded-lg p-6">
                    <div className="flex items-start justify-between mb-3">
                      <h3 className="font-semibold text-gray-900">{template.name}</h3>
                      <span className="px-2 py-1 text-xs bg-blue-100 text-blue-800 rounded">
                        {template.category.replace('_', ' ')}
                      </span>
                    </div>
                    <p className="text-sm text-gray-600 mb-4">{template.message}</p>
                    <div className="flex gap-2">
                      <button className="text-sm text-primary-600 hover:text-primary-700">
                        Use Template
                      </button>
                      <button className="text-sm text-gray-600 hover:text-gray-700">
                        Edit
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>

      {/* New Campaign Modal */}
      {showNewCampaign && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-lg shadow-xl max-w-2xl w-full p-6">
            <h2 className="text-2xl font-bold text-gray-900 mb-4">Create New Campaign</h2>

            <div className="space-y-4 mb-6">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Campaign Name
                </label>
                <input
                  type="text"
                  value={newCampaignName}
                  onChange={(e) => setNewCampaignName(e.target.value)}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
                  placeholder="e.g., Weekly Review Request"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Message
                </label>
                <textarea
                  value={newCampaignMessage}
                  onChange={(e) => setNewCampaignMessage(e.target.value)}
                  rows={6}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
                  placeholder="Use {name}, {business_name}, {review_link} as placeholders"
                />
                <p className="text-sm text-gray-500 mt-1">
                  Available variables: {'{name}'}, {'{business_name}'}, {'{review_link}'}
                </p>
              </div>
            </div>

            <div className="flex gap-3 justify-end">
              <button
                onClick={() => {
                  setShowNewCampaign(false);
                  setNewCampaignName('');
                  setNewCampaignMessage('');
                }}
                className="px-4 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200"
              >
                Cancel
              </button>
              <button
                onClick={createCampaign}
                className="btn-primary"
              >
                Create Campaign
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
