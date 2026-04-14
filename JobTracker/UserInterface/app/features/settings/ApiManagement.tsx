'use client';

import React, { useState } from 'react';
import { TextInput, Switch } from '@mantine/core';
import {
  IconMessage,
  IconCheck,
  IconX,
  IconKey,
  IconTestPipe,
} from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';
import { sendPhotinoRequest } from '@/app/utils/photino';
import { Settings } from '@/app/types/settings/settings';
import { UpdateSettingsRequest } from '@/app/types/settings/update-settings-request';
import { UpdateSettingsResponse } from '@/app/types/settings/update-settings-response';
import { TestConnectionResponse } from '@/app/types/settings/test-connection-response';
import { useSettings } from '@/app/hooks/useSettings';

interface ApiManagementProps {
  className?: string;
  settings: Settings | null;
}

const inputCls = {
  input: 'bg-neutral-800 border-neutral-700 text-neutral-200 placeholder-neutral-500',
  error: 'text-red-400',
};

export default function ApiManagement({ className }: ApiManagementProps) {
  const [discordWebhookUrl, setDiscordWebhookUrl] = useState('');
  const [discordNotificationsEnabled, setDiscordNotificationsEnabled] = useState(false);
  const [discordLoading, setDiscordLoading] = useState(false);
  const [discordTesting, setDiscordTesting] = useState(false);
  const [discordWebhookUrlError, setDiscordWebhookUrlError] = useState('');

  const { data: settings } = useSettings();

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
        GenerateEmbeddings: null,
        FirstStart: null,
      };
      await sendPhotinoRequest<UpdateSettingsResponse>('settings.updateSettings', request);
      notifications.show({
        title: 'Success',
        message: 'Discord configuration saved successfully',
        color: 'green',
        icon: <IconCheck size={16} />,
      });
    } catch (err) {
      console.error('Failed to save Discord config:', err);
      notifications.show({
        title: 'Error',
        message: 'Failed to save Discord configuration',
        color: 'red',
        icon: <IconX size={16} />,
      });
    } finally {
      setDiscordLoading(false);
    }
  };

  const handleTestDiscord = async () => {
    try {
      setDiscordTesting(true);
      const response = await sendPhotinoRequest<TestConnectionResponse>('settings.testConnection', {
        WebhookUrl: discordWebhookUrl,
      });
      notifications.show({
        title: 'Test Result',
        message: response.Success ? 'Discord webhook test completed successfully' : 'Discord webhook test failed',
        color: response.Success ? 'green' : 'red',
        icon: response.Success ? <IconCheck size={16} /> : <IconX size={16} />,
      });
    } catch (err) {
      console.error('Failed to test Discord connection:', err);
      notifications.show({
        title: 'Test Failed',
        message: 'Discord webhook test failed',
        color: 'red',
        icon: <IconX size={16} />,
      });
    } finally {
      setDiscordTesting(false);
    }
  };

  return (
    <div className={className}>
      <div className="mb-4">
        <p className="text-sm font-semibold text-neutral-300">API Management</p>
      </div>

      <div className="rounded-xl border border-neutral-800 bg-neutral-900/50 divide-y divide-neutral-800">
        {/* Discord header row */}
        <div className="px-4 py-3.5 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <IconMessage size={16} className="text-neutral-400 flex-shrink-0" />
            <div>
              <span className="text-sm font-medium text-neutral-200">Discord Webhook</span>
              <span className="ml-3 text-xs text-neutral-500">Job notifications via Discord</span>
            </div>
          </div>
          <Switch
            label="Enable"
            checked={discordNotificationsEnabled}
            onChange={(event) => setDiscordNotificationsEnabled(event.currentTarget.checked)}
            size="sm"
            classNames={{ label: 'text-neutral-400 text-xs', track: 'bg-neutral-700' }}
          />
        </div>

        {/* Webhook URL */}
        <div className="px-4 py-4">
          <p className="text-xs font-medium text-neutral-500 uppercase tracking-wide mb-1.5">Webhook URL</p>
          <TextInput
            placeholder="https://discord.com/api/webhooks/..."
            value={discordWebhookUrl}
            onChange={(event) => setDiscordWebhookUrl(event.currentTarget.value)}
            error={discordWebhookUrlError}
            disabled={!discordNotificationsEnabled}
            leftSection={<IconKey size={15} />}
            classNames={inputCls}
          />
          <div className="flex justify-end gap-2 mt-4">
            <button
              onClick={handleTestDiscord}
              disabled={!discordNotificationsEnabled || discordTesting || !discordWebhookUrl.trim()}
              className="btn-ghost text-sm flex items-center gap-2"
            >
              <IconTestPipe size={15} />
              Test Webhook
            </button>
            <button
              onClick={handleSaveDiscordConfig}
              disabled={discordLoading}
              className="btn-secondary text-sm flex items-center gap-2"
            >
              <IconCheck size={15} />
              Save
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
