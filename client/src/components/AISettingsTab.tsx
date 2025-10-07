import { Save, Info, AlertCircle } from 'lucide-react';

interface AISettingsProps {
  settings: {
    enableAutoReply: boolean;
    autoReplyToPositiveReviews: boolean;
    autoReplyToNeutralReviews: boolean;
    autoReplyToQuestions: boolean;
    enableAISuggestions: boolean;
    enableSentimentAnalysis: boolean;
    enableCompetitorAnalysis: boolean;
    enableInsightsGeneration: boolean;
    responseTone: string;
    responseLength: string;
  };
  onUpdate: (settings: any) => void;
  onSave: () => void;
  saving: boolean;
}

export function AISettingsTab({ settings, onUpdate, onSave, saving }: AISettingsProps) {
  return (
    <div className="bg-white rounded-lg shadow-md border border-gray-200 p-6">
      <div className="flex items-center gap-3 mb-6">
        <h2 className="text-xl font-semibold text-gray-900">AI Features</h2>
        <span className="px-3 py-1 bg-gradient-to-r from-purple-100 to-blue-100 text-purple-700 text-xs font-semibold rounded-full">
          Powered by OpenAI
        </span>
      </div>

      <div className="space-y-8">
        {/* Auto-Reply Settings */}
        <div>
          <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center gap-2">
            Auto-Reply
            <Info className="w-4 h-4 text-gray-400" />
          </h3>

          <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4 mb-4">
            <div className="flex gap-3">
              <AlertCircle className="w-5 h-5 text-yellow-600 flex-shrink-0 mt-0.5" />
              <div>
                <p className="text-sm font-medium text-yellow-900 mb-1">Safety First</p>
                <p className="text-sm text-yellow-800">
                  Negative reviews (2 stars or less) will never be auto-replied. They will be flagged for your manual review.
                </p>
              </div>
            </div>
          </div>

          <div className="space-y-4">
            <label className="flex items-start gap-3 cursor-pointer">
              <input
                type="checkbox"
                checked={settings.enableAutoReply}
                onChange={(e) => onUpdate({ ...settings, enableAutoReply: e.target.checked })}
                className="mt-1 w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
              />
              <div>
                <p className="text-sm font-medium text-gray-900">Enable Auto-Reply</p>
                <p className="text-sm text-gray-500">
                  Automatically respond to reviews based on your settings below
                </p>
              </div>
            </label>

            {settings.enableAutoReply && (
              <div className="ml-7 space-y-3 pl-4 border-l-2 border-gray-200">
                <label className="flex items-start gap-3 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={settings.autoReplyToPositiveReviews}
                    onChange={(e) => onUpdate({ ...settings, autoReplyToPositiveReviews: e.target.checked })}
                    className="mt-1 w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
                  />
                  <div>
                    <p className="text-sm font-medium text-gray-900">Positive Reviews (4-5 stars)</p>
                    <p className="text-sm text-gray-500">Auto-reply to positive reviews</p>
                  </div>
                </label>

                <label className="flex items-start gap-3 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={settings.autoReplyToNeutralReviews}
                    onChange={(e) => onUpdate({ ...settings, autoReplyToNeutralReviews: e.target.checked })}
                    className="mt-1 w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
                  />
                  <div>
                    <p className="text-sm font-medium text-gray-900">Neutral Reviews (3 stars)</p>
                    <p className="text-sm text-gray-500">Auto-reply to neutral reviews</p>
                  </div>
                </label>

                <label className="flex items-start gap-3 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={settings.autoReplyToQuestions}
                    onChange={(e) => onUpdate({ ...settings, autoReplyToQuestions: e.target.checked })}
                    className="mt-1 w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
                  />
                  <div>
                    <p className="text-sm font-medium text-gray-900">Reviews with Questions</p>
                    <p className="text-sm text-gray-500">
                      Auto-reply when AI detects a question, regardless of rating
                    </p>
                  </div>
                </label>
              </div>
            )}
          </div>
        </div>

        {/* Response Style */}
        <div className="pt-6 border-t border-gray-200">
          <h3 className="text-lg font-medium text-gray-900 mb-4">Response Style</h3>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Tone
              </label>
              <select
                value={settings.responseTone}
                onChange={(e) => onUpdate({ ...settings, responseTone: e.target.value })}
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
              >
                <option value="Professional">Professional</option>
                <option value="Friendly">Friendly</option>
                <option value="Casual">Casual</option>
              </select>
              <p className="mt-1 text-xs text-gray-500">
                How formal should the responses be?
              </p>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Length
              </label>
              <select
                value={settings.responseLength}
                onChange={(e) => onUpdate({ ...settings, responseLength: e.target.value })}
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
              >
                <option value="Short">Short (1-2 sentences)</option>
                <option value="Medium">Medium (2-3 sentences)</option>
                <option value="Long">Long (4-5 sentences)</option>
              </select>
              <p className="mt-1 text-xs text-gray-500">
                How detailed should responses be?
              </p>
            </div>
          </div>
        </div>

        {/* AI Assistance Features */}
        <div className="pt-6 border-t border-gray-200">
          <h3 className="text-lg font-medium text-gray-900 mb-4">AI Assistance Features</h3>
          <div className="space-y-4">
            <label className="flex items-start gap-3 cursor-pointer">
              <input
                type="checkbox"
                checked={settings.enableAISuggestions}
                onChange={(e) => onUpdate({ ...settings, enableAISuggestions: e.target.checked })}
                className="mt-1 w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
              />
              <div>
                <p className="text-sm font-medium text-gray-900">AI Response Suggestions</p>
                <p className="text-sm text-gray-500">
                  Get AI-generated response suggestions for manual replies
                </p>
              </div>
            </label>

            <label className="flex items-start gap-3 cursor-pointer">
              <input
                type="checkbox"
                checked={settings.enableSentimentAnalysis}
                onChange={(e) => onUpdate({ ...settings, enableSentimentAnalysis: e.target.checked })}
                className="mt-1 w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
              />
              <div>
                <p className="text-sm font-medium text-gray-900">Sentiment Analysis</p>
                <p className="text-sm text-gray-500">
                  Automatically analyze review sentiment and extract keywords
                </p>
              </div>
            </label>

            <label className="flex items-start gap-3 cursor-pointer">
              <input
                type="checkbox"
                checked={settings.enableCompetitorAnalysis}
                onChange={(e) => onUpdate({ ...settings, enableCompetitorAnalysis: e.target.checked })}
                className="mt-1 w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
              />
              <div>
                <p className="text-sm font-medium text-gray-900">Competitor Analysis</p>
                <p className="text-sm text-gray-500">
                  Get AI-powered insights about your competitive position
                </p>
              </div>
            </label>

            <label className="flex items-start gap-3 cursor-pointer">
              <input
                type="checkbox"
                checked={settings.enableInsightsGeneration}
                onChange={(e) => onUpdate({ ...settings, enableInsightsGeneration: e.target.checked })}
                className="mt-1 w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
              />
              <div>
                <p className="text-sm font-medium text-gray-900">Analytics Insights</p>
                <p className="text-sm text-gray-500">
                  Generate actionable insights and recommendations from your data
                </p>
              </div>
            </label>
          </div>
        </div>

        {/* Save Button */}
        <div className="flex justify-end pt-6 border-t border-gray-200">
          <button
            onClick={onSave}
            disabled={saving}
            className="btn-primary flex items-center gap-2"
          >
            <Save className="w-4 h-4" />
            {saving ? 'Saving...' : 'Save AI Settings'}
          </button>
        </div>
      </div>
    </div>
  );
}
