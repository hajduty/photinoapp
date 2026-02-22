'use client'

import React, { useState, useEffect } from 'react'
import {
  Box,
  Title,
  Text,
  Space,
  Flex,
  Switch,
  Loader
} from '@mantine/core'
import { sendPhotinoRequest } from '@/app/utils/photino'
import { Settings } from '@/app/types/settings/settings'
import { UpdateSettingsRequest } from '@/app/types/settings/update-settings-request'
import { UpdateSettingsResponse } from '@/app/types/settings/update-settings-response'
import TagManagement from '../../features/settings/TagManagement'
import ApiManagement from '../../features/settings/ApiManagement'

export default function SettingsPage() {
  const [embeddingsEnabled, setEmbeddingsEnabled] = useState(false);
  const [settings, setSettings] = useState<Settings | null>(null);
  const [settingsLoading, setSettingsLoading] = useState(true);
  const [settingsError, setSettingsError] = useState<string | null>(null);
  const [embeddingsLoading, setEmbeddingsLoading] = useState(false);

  useEffect(() => {
    const fetchSettings = async () => {
      try {
        setSettingsLoading(true);
        setSettingsError(null);
        
        const response = await sendPhotinoRequest<Settings>('settings.getSettings', {});
        setSettings(response);
        setEmbeddingsEnabled(response.GenerateEmbeddings ?? false);
        console.log(response);
      } catch (err) {
        console.error('Failed to fetch settings:', err);
        setSettingsError('Failed to load settings');
      } finally {
        setSettingsLoading(false);
      }
    };

    fetchSettings();
  }, []);

  const handleEmbeddingsToggle = async (enabled: boolean) => {
    try {
      setEmbeddingsLoading(true);
      
      const request: UpdateSettingsRequest = {
        DiscordWebhookUrl: settings?.DiscordWebhookUrl ?? '',
        DiscordNotificationsEnabled: settings?.DiscordNotificationsEnabled ?? false,
        GenerateEmbeddings: enabled
      };

      await sendPhotinoRequest<UpdateSettingsResponse>('settings.updateSettings', request);
      
      setEmbeddingsEnabled(enabled);
      setSettings(prev => prev ? { ...prev, GenerateEmbeddings: enabled } : null);
    } catch (err) {
      console.error('Failed to update embeddings setting:', err);
    } finally {
      setEmbeddingsLoading(false);
    }
  };

  if (settingsLoading) {
    return (
      <div className="p-8">
        <div className="max-w-7xl mx-auto py-6">
          <Flex justify="center" align="center" style={{ minHeight: '200px' }}>
            <Loader />
          </Flex>
        </div>
      </div>
    );
  }

  return (
    <div className="p-4 md:p-8">
      <div className="max-w-7xl mx-auto space-y-8">
        <Flex justify="space-between" align="center" mb="md">
          <Box>
            <Title order={2} c="white">SETTINGS</Title>
            <Text c="dimmed">Manage settings and preferences</Text>
          </Box>
        </Flex>

        <TagManagement />

        <Space h="xl" />

        <ApiManagement settings={settings} />

        <Switch
          label="Enable AI features"
          checked={embeddingsEnabled}
          onChange={(event) => handleEmbeddingsToggle(event.currentTarget.checked)}
          disabled={embeddingsLoading}
          size="sm"
          classNames={{
            label: 'text-neutral-300',
            track: 'bg-neutral-700'
          }}
        />
      </div>
    </div>
  )
}
