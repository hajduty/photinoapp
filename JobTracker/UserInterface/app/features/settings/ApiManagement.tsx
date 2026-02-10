'use client';

import React, { useState, useEffect } from 'react';
import {
  TextInput,
  Space,
  Alert,
  Loader,
  Switch,
  Select
} from '@mantine/core';
import {
  IconMessage,
  IconCheck,
  IconX,
  IconAlertCircle,
  IconKey,
  IconTestPipe
} from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';

interface ApiManagementProps {
  className?: string;
}

interface GmailConfig {
  clientId: string;
  clientSecret: string;
  refreshToken: string;
  enabled: boolean;
}

interface DiscordConfig {
  webhookUrl: string;
  enabled: boolean;
  notificationTypes: string[];
}

export default function ApiManagement({ className }: ApiManagementProps) {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Gmail API State
  const [gmailConfig, setGmailConfig] = useState<GmailConfig>({
    clientId: '',
    clientSecret: '',
    refreshToken: '',
    enabled: false
  });
  const [gmailLoading, setGmailLoading] = useState(false);
  const [gmailTesting, setGmailTesting] = useState(false);

  // Discord API State
  const [discordConfig, setDiscordConfig] = useState<DiscordConfig>({
    webhookUrl: '',
    enabled: false,
    notificationTypes: []
  });
  const [discordLoading, setDiscordLoading] = useState(false);
  const [discordTesting, setDiscordTesting] = useState(false);

  // Form validation
  const [gmailErrors, setGmailErrors] = useState({
    clientId: '',
    clientSecret: '',
    refreshToken: ''
  });
  const [discordErrors, setDiscordErrors] = useState({
    webhookUrl: ''
  });

  useEffect(() => {
    fetchConfigs();
  }, []);

  const fetchConfigs = async () => {
    try {
      setLoading(true);
      setError(null);
      
      // TODO: Replace with actual API calls
      // const gmailResponse = await sendPhotinoRequest<GmailConfig>('api.getGmailConfig', {});
      // const discordResponse = await sendPhotinoRequest<DiscordConfig>('api.getDiscordConfig', {});
      
      // For now, using mock data
      setGmailConfig({
        clientId: 'mock_client_id',
        clientSecret: 'mock_client_secret',
        refreshToken: 'mock_refresh_token',
        enabled: false
      });
      
      setDiscordConfig({
        webhookUrl: 'https://discord.com/api/webhooks/mock/webhook',
        enabled: false,
        notificationTypes: ['new_jobs', 'tag_updates']
      });
    } catch (err) {
      console.error('Failed to fetch API configs:', err);
      setError('Failed to load API configurations. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const validateGmailConfig = (): boolean => {
    const errors = {
      clientId: !gmailConfig.clientId.trim() ? 'Client ID is required' : '',
      clientSecret: !gmailConfig.clientSecret.trim() ? 'Client Secret is required' : '',
      refreshToken: !gmailConfig.refreshToken.trim() ? 'Refresh Token is required' : ''
    };
    
    setGmailErrors(errors);
    return !errors.clientId && !errors.clientSecret && !errors.refreshToken;
  };

  const validateDiscordConfig = (): boolean => {
    const errors = {
      webhookUrl: !discordConfig.webhookUrl.trim() ? 'Webhook URL is required' : ''
    };
    
    setDiscordErrors(errors);
    return !errors.webhookUrl;
  };

  const handleSaveGmailConfig = async () => {
    if (!validateGmailConfig()) return;

    try {
      setGmailLoading(true);
      // TODO: Replace with actual API call
      // const response = await sendPhotinoRequest('api.saveGmailConfig', gmailConfig);
      
      notifications.show({
        title: 'Success',
        message: 'Gmail API configuration saved successfully',
        color: 'green',
        icon: <IconCheck size={16} />
      });
    } catch (err) {
      console.error('Failed to save Gmail config:', err);
      notifications.show({
        title: 'Error',
        message: 'Failed to save Gmail API configuration',
        color: 'red',
        icon: <IconX size={16} />
      });
    } finally {
      setGmailLoading(false);
    }
  };

  const handleSaveDiscordConfig = async () => {
    if (!validateDiscordConfig()) return;

    try {
      setDiscordLoading(true);
      // TODO: Replace with actual API call
      // const response = await sendPhotinoRequest('api.saveDiscordConfig', discordConfig);
      
      notifications.show({
        title: 'Success',
        message: 'Discord API configuration saved successfully',
        color: 'green',
        icon: <IconCheck size={16} />
      });
    } catch (err) {
      console.error('Failed to save Discord config:', err);
      notifications.show({
        title: 'Error',
        message: 'Failed to save Discord API configuration',
        color: 'red',
        icon: <IconX size={16} />
      });
    } finally {
      setDiscordLoading(false);
    }
  };

  const handleTestGmail = async () => {
    try {
      setGmailTesting(true);
      // TODO: Replace with actual API call
      // const response = await sendPhotinoRequest('api.testGmailConnection', {});
      
      notifications.show({
        title: 'Test Result',
        message: 'Gmail API connection test completed successfully',
        color: 'green',
        icon: <IconCheck size={16} />
      });
    } catch (err) {
      console.error('Failed to test Gmail connection:', err);
      notifications.show({
        title: 'Test Failed',
        message: 'Gmail API connection test failed',
        color: 'red',
        icon: <IconX size={16} />
      });
    } finally {
      setGmailTesting(false);
    }
  };

  const handleTestDiscord = async () => {
    try {
      setDiscordTesting(true);
      // TODO: Replace with actual API call
      // const response = await sendPhotinoRequest('api.testDiscordConnection', {});
      
      notifications.show({
        title: 'Test Result',
        message: 'Discord webhook test completed successfully',
        color: 'green',
        icon: <IconCheck size={16} />
      });
    } catch (err) {
      console.error('Failed to test Discord connection:', err);
      notifications.show({
        title: 'Test Failed',
        message: 'Discord webhook test failed',
        color: 'red',
        icon: <IconX size={16} />
      });
    } finally {
      setDiscordTesting(false);
    }
  };

  if (loading) {
    return (
      <div className={className}>
        <Loader />
      </div>
    );
  }

  return (
    <div className={className}>
      {/* Header */}
      <div className="py-6">
        <div className="flex items-start justify-between">
          <div>
            <h1 className="text-lg font-bold text-neutral-200 mb-2">API MANAGEMENT</h1>
            <p className="text-neutral-400">Configure external API integrations for notifications and automation</p>
          </div>
        </div>
      </div>

      {/* Error State */}
      {error && (
        <Alert 
          icon={<IconAlertCircle size={16} />} 
          title="Error" 
          color="red" 
          mb="md"
        >
          {error}
        </Alert>
      )}

      <Space h="md" />

      <div>
        <div className="flex justify-between items-center mb-4">
          <div>
            <h2 className="text font-semibold text-neutral-200 flex items-center gap-2">
              Gmail API Integration
            </h2>
            <p className="text-neutral-400 text-sm">
              Configure Gmail API for email notifications and job application tracking
            </p>
          </div>
          <Switch
            label="Enable Gmail Integration"
            checked={gmailConfig.enabled}
            onChange={(event) => setGmailConfig(prev => ({ ...prev, enabled: event.currentTarget.checked }))}
            size="sm"
            classNames={{
              label: 'text-neutral-300',
              track: 'bg-neutral-700'
            }}
          />
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
          <TextInput
            label="Client ID"
            placeholder="Enter your Gmail API Client ID"
            value={gmailConfig.clientId}
            onChange={(event) => setGmailConfig(prev => ({ ...prev, clientId: event.currentTarget.value }))}
            error={gmailErrors.clientId}
            disabled={!gmailConfig.enabled}
            leftSection={<IconKey size={16} />}
            classNames={{
              input: 'bg-neutral-800 border-neutral-600 text-neutral-200 placeholder-neutral-500',
              label: 'text-neutral-300',
              error: 'text-red-400'
            }}
          />
          
          <TextInput
            label="Client Secret"
            placeholder="Enter your Gmail API Client Secret"
            value={gmailConfig.clientSecret}
            onChange={(event) => setGmailConfig(prev => ({ ...prev, clientSecret: event.currentTarget.value }))}
            error={gmailErrors.clientSecret}
            disabled={!gmailConfig.enabled}
            leftSection={<IconKey size={16} />}
            classNames={{
              input: 'bg-neutral-800 border-neutral-600 text-neutral-200 placeholder-neutral-500',
              label: 'text-neutral-300',
              error: 'text-red-400'
            }}
          />
        </div>

        <TextInput
          label="Refresh Token"
          placeholder="Enter your Gmail API Refresh Token"
          value={gmailConfig.refreshToken}
          onChange={(event) => setGmailConfig(prev => ({ ...prev, refreshToken: event.currentTarget.value }))}
          error={gmailErrors.refreshToken}
          disabled={!gmailConfig.enabled}
          leftSection={<IconKey size={16} />}
          classNames={{
            input: 'bg-neutral-800 border-neutral-600 text-neutral-200 placeholder-neutral-500',
            label: 'text-neutral-300',
            error: 'text-red-400'
          }}
        />

        <div className="flex justify-end gap-2 mt-4">
          <button 
            onClick={handleTestGmail}
            disabled={!gmailConfig.enabled || gmailTesting}
            className="btn-ghost text-sm flex items-center gap-2"
          >
            <IconTestPipe size={16} />
            Test Connection
          </button>
          <button 
            onClick={handleSaveGmailConfig}
            disabled={!gmailConfig.enabled || gmailLoading}
            className="btn-secondary text-sm flex items-center gap-2"
          >
            <IconCheck size={16} />
            Save Configuration
          </button>
        </div>
      </div>

      {/* Discord API Section */}
      <div>
        <div className="flex justify-between items-center mb-4">
          <div>
            <h2 className="text-lg font-semibold text-neutral-200 flex items-center gap-2">
              <IconMessage size={20} />
              Discord Webhook Integration
            </h2>
            <p className="text-neutral-400 text-sm">
              Configure Discord webhooks for job notifications and updates
            </p>
          </div>
          <Switch
            label="Enable Discord Integration"
            checked={discordConfig.enabled}
            onChange={(event) => setDiscordConfig(prev => ({ ...prev, enabled: event.currentTarget.checked }))}
            size="sm"
            classNames={{
              label: 'text-neutral-300',
              track: 'bg-neutral-700'
            }}
          />
        </div>

        <TextInput
          label="Webhook URL"
          placeholder="https://discord.com/api/webhooks/..."
          value={discordConfig.webhookUrl}
          onChange={(event) => setDiscordConfig(prev => ({ ...prev, webhookUrl: event.currentTarget.value }))}
          error={discordErrors.webhookUrl}
          disabled={!discordConfig.enabled}
          leftSection={<IconKey size={16} />}
          classNames={{
            input: 'bg-neutral-800 border-neutral-600 text-neutral-200 placeholder-neutral-500',
            label: 'text-neutral-300',
            error: 'text-red-400'
          }}
        />

        <div className="mb-4">
          <Select
            label="Notification Types"
            placeholder="Select notification types"
            value={discordConfig.notificationTypes as any}
            onChange={(value) => setDiscordConfig(prev => ({ ...prev, notificationTypes: Array.isArray(value) ? value : [] }))}
            data={[
              { value: 'new_jobs', label: 'New Job Postings' },
              { value: 'tag_updates', label: 'Tag Updates' },
              { value: 'application_status', label: 'Application Status Changes' },
              { value: 'daily_summary', label: 'Daily Summary' }
            ]}
            disabled={!discordConfig.enabled}
            multiple
            searchable
            clearable
            classNames={{
              input: 'bg-neutral-800 border-neutral-600 text-neutral-200',
              label: 'text-neutral-300',
              dropdown: 'bg-neutral-800 border-neutral-600',
              option: 'text-neutral-200 hover:bg-neutral-700'
            }}
          />
        </div>

        <div className="flex justify-end gap-2">
          <button 
            onClick={handleTestDiscord}
            disabled={!discordConfig.enabled || discordTesting}
            className="btn-ghost text-sm flex items-center gap-2"
          >
            <IconTestPipe size={16} />
            Test Webhook
          </button>
          <button 
            onClick={handleSaveDiscordConfig}
            disabled={!discordConfig.enabled || discordLoading}
            className="btn-secondary text-sm flex items-center gap-2"
          >
            <IconCheck size={16} />
            Save Configuration
          </button>
        </div>
      </div>
    </div>
  );
}