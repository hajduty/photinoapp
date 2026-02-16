'use client';

import React, { useState, useEffect } from 'react';
import {
  TextInput,
  Space,
  Alert,
  Loader,
  Switch
} from '@mantine/core';
import {
  IconMessage,
  IconCheck,
  IconX,
  IconAlertCircle,
  IconKey,
  IconTestPipe,
} from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';
import { sendPhotinoRequest } from '@/app/utils/photino';
import { Settings } from '@/app/types/settings/settings';
import { UpdateSettingsRequest } from '@/app/types/settings/update-settings-request';
import { UpdateSettingsResponse } from '@/app/types/settings/update-settings-response';
import { TestConnectionResponse } from '@/app/types/settings/test-connection-response';

interface ApiManagementProps {
  className?: string;
  settings: Settings | null;
}

export default function ApiManagement({ className, settings }: ApiManagementProps) {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Discord State
  const [discordWebhookUrl, setDiscordWebhookUrl] = useState('');
  const [discordNotificationsEnabled, setDiscordNotificationsEnabled] = useState(false);
  const [discordLoading, setDiscordLoading] = useState(false);
  const [discordTesting, setDiscordTesting] = useState(false);

  // Form validation
  const [discordWebhookUrlError, setDiscordWebhookUrlError] = useState('');

  useEffect(() => {
    if (settings) {
      setDiscordWebhookUrl(settings.DiscordWebhookUrl ?? '');
      setDiscordNotificationsEnabled(settings.DiscordNotificationsEnabled ?? false);
      setLoading(false);
    } else if (!settings) {
      setError('Settings not available');
      setLoading(false);
    }
  }, [settings]);

  const validateDiscordConfig = (): boolean => {
    const error = !discordWebhookUrl.trim() ? 'Webhook URL is required' : '';
    setDiscordWebhookUrlError(error);
    return !error;
  };

  const handleSaveDiscordConfig = async () => {
    if (!validateDiscordConfig()) return;

    try {
      setDiscordLoading(true);

      const request: UpdateSettingsRequest = {
        DiscordWebhookUrl: discordWebhookUrl,
        DiscordNotificationsEnabled: discordNotificationsEnabled,
        GenerateEmbeddings: null
      };

      await sendPhotinoRequest<UpdateSettingsResponse>('settings.updateSettings', request);

      notifications.show({
        title: 'Success',
        message: 'Discord configuration saved successfully',
        color: 'green',
        icon: <IconCheck size={16} />
      });
    } catch (err) {
      console.error('Failed to save Discord config:', err);
      notifications.show({
        title: 'Error',
        message: 'Failed to save Discord configuration',
        color: 'red',
        icon: <IconX size={16} />
      });
    } finally {
      setDiscordLoading(false);
    }
  };

  const handleTestDiscord = async () => {
    try {
      setDiscordTesting(true);

      const request = {
        WebhookUrl: discordWebhookUrl
      };

      var response = await sendPhotinoRequest<TestConnectionResponse>('settings.testConnection', request);

      if (response.Success) {
        notifications.show({
          title: 'Test Result',
          message: 'Discord webhook test completed successfully',
          color: 'green',
          icon: <IconCheck size={16} />
        });
      } else {
        notifications.show({
          title: 'Test Result',
          message: 'Discord webhook test failed',
          color: 'red',
          icon: <IconX size={16} />
        });
      }
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

      {/* Discord Webhook Section */}
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
            checked={discordNotificationsEnabled}
            onChange={(event) => setDiscordNotificationsEnabled(event.currentTarget.checked)}
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
          value={discordWebhookUrl}
          onChange={(event) => setDiscordWebhookUrl(event.currentTarget.value)}
          error={discordWebhookUrlError}
          disabled={!discordNotificationsEnabled}
          leftSection={<IconKey size={16} />}
          classNames={{
            input: 'bg-neutral-800 border-neutral-600 text-neutral-200 placeholder-neutral-500',
            label: 'text-neutral-300',
            error: 'text-red-400'
          }}
        />

        <div className="flex justify-end gap-2 mt-4">
          <button
            onClick={handleTestDiscord}
            disabled={!discordNotificationsEnabled || discordTesting || !discordWebhookUrl.trim()}
            className="btn-ghost text-sm flex items-center gap-2"
          >
            <IconTestPipe size={16} />
            Test Webhook
          </button>
          <button
            onClick={handleSaveDiscordConfig}
            disabled={discordLoading}
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
