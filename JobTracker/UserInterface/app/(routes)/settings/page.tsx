'use client'

import React, { useState, useEffect } from 'react'
import {
  Switch,
  Loader,
  Divider,
} from '@mantine/core'
import { IconSparkles } from '@tabler/icons-react'
import { sendPhotinoRequest } from '@/app/utils/photino'
import { Settings } from '@/app/types/settings/settings'
import { UpdateSettingsRequest } from '@/app/types/settings/update-settings-request'
import { UpdateSettingsResponse } from '@/app/types/settings/update-settings-response'
import TagManagement from '../../features/settings/TagManagement'
import ApiManagement from '../../features/settings/ApiManagement'
import CVManagement from '../../features/settings/CVManagement'
import UserPreferences from '../../features/settings/UserPreferences'
import IgnoredJobsSection from '../../features/settings/IgnoredJobsModal'

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
        GenerateEmbeddings: enabled,
        UserCV: settings?.UserCV ?? ''
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
        <div className="max-w-7xl mx-auto py-6 flex items-center justify-center" style={{ minHeight: '200px' }}>
          <Loader />
        </div>
      </div>
    );
  }

  return (
    <div className="p-4 md:p-8">
      <div className="max-w-7xl mx-auto space-y-8">
        <div className="mb-2">
          <h1 className="text-2xl font-bold text-white mb-1">Settings</h1>
          <p className="text-sm text-neutral-500">Manage settings and preferences</p>
        </div>

        <ApiManagement settings={settings} />

        <div>
          <div className="mb-4">
            <p className="text-sm font-semibold text-neutral-300">CV & Job Preferences</p>
          </div>

          <div className="space-y-3">
            <CVManagement settings={settings} onUpdate={setSettings} />
            <UserPreferences settings={settings} onUpdate={setSettings} />
            <IgnoredJobsSection />

            {/* AI features — card row matching siblings */}
            <div className="rounded-xl border border-neutral-800 bg-neutral-900/50 px-4 py-3.5 flex items-center justify-between">
              <div className="flex items-center gap-3">
                <IconSparkles size={16} className="text-neutral-400 flex-shrink-0" />
                <div>
                  <span className="text-sm font-medium text-neutral-200">AI Features</span>
                  <span className="ml-3 text-xs text-neutral-500">Embedding-based job recommendations</span>
                </div>
              </div>
              <Switch
                checked={embeddingsEnabled}
                onChange={(event) => handleEmbeddingsToggle(event.currentTarget.checked)}
                disabled={embeddingsLoading}
                size="sm"
                classNames={{ track: 'bg-neutral-700' }}
              />
            </div>
          </div>
        </div>

        <TagManagement />

      </div>
    </div>
  )
}
