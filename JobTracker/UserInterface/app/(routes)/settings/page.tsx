'use client'

import React, { useState } from 'react'
import { Switch, Loader } from '@mantine/core'
import { IconSparkles, IconPlus, IconTrash, IconBriefcase } from '@tabler/icons-react'
import { sendPhotinoRequest } from '@/app/utils/photino'
import { UpdateSettingsRequest } from '@/app/types/settings/update-settings-request'
import { UpdateSettingsResponse } from '@/app/types/settings/update-settings-response'
import { JobProfile } from '@/app/types/settings/job-profile'
import { useSettings } from '@/app/hooks/useSettings'
import { useCreateProfile, useDeleteProfile, useSwitchProfile } from '@/app/hooks/useProfiles'
import TagManagement from '../../features/settings/TagManagement'
import ApiManagement from '../../features/settings/ApiManagement'
import CVManagement from '../../features/settings/CVManagement'
import UserPreferences from '../../features/settings/UserPreferences'
import IgnoredJobsSection from '../../features/settings/IgnoredJobsModal'

export default function SettingsPage() {
  const { data: settingsData, isLoading: settingsLoading } = useSettings()
  const [embeddingsLoading, setEmbeddingsLoading] = useState(false)
  const [newProfileName, setNewProfileName] = useState('')
  const [creatingProfile, setCreatingProfile] = useState(false)

  const createProfile = useCreateProfile()
  const switchProfile = useSwitchProfile()
  const deleteProfile = useDeleteProfile()

  const settings = settingsData?.Settings ?? null
  const profiles = settingsData?.Profiles ?? []
  const activeProfile = settingsData?.ActiveProfile ?? null

  const handleProfileUpdate = (updated: JobProfile) => {
    // The query will be invalidated by the mutation; for immediate UI update
    // we rely on re-fetch. Nothing needed here since useSettings auto-refreshes.
  }

  const handleEmbeddingsToggle = async (enabled: boolean) => {
    try {
      setEmbeddingsLoading(true)
      const request: UpdateSettingsRequest = {
        DiscordWebhookUrl: null,
        DiscordNotificationsEnabled: null,
        GenerateEmbeddings: enabled,
        FirstStart: null,
      }
      await sendPhotinoRequest<UpdateSettingsResponse>('settings.updateSettings', request)
    } catch (err) {
      console.error('Failed to update embeddings setting:', err)
    } finally {
      setEmbeddingsLoading(false)
    }
  }

  const handleCreateProfile = async () => {
    const name = newProfileName.trim() || 'New Profile'
    await createProfile.mutateAsync({ Name: name })
    setNewProfileName('')
    setCreatingProfile(false)
  }

  const handleDeleteProfile = async (profileId: number) => {
    if (profiles.length <= 1) return
    await deleteProfile.mutateAsync({ ProfileId: profileId })
  }

  if (settingsLoading) {
    return (
      <div className="p-8">
        <div className="max-w-7xl mx-auto py-6 flex items-center justify-center" style={{ minHeight: '200px' }}>
          <Loader />
        </div>
      </div>
    )
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
          {/* Section header + profile switcher */}
          <div className="mb-4 flex items-center justify-between gap-4">
            <p className="text-sm font-semibold text-neutral-300">CV & Job Preferences</p>

            <div className="flex items-center gap-2">
              {/* Profile tabs */}
              <div className="flex items-center gap-1 bg-neutral-900 border border-neutral-800 rounded-lg p-1">
                {profiles.map(p => (
                  <div key={p.Id} className="flex items-center group">
                    <button
                      onClick={() => switchProfile.mutate({ ProfileId: p.Id })}
                      className={`flex items-center gap-1.5 px-3 py-1.5 rounded-md text-xs font-medium transition-colors ${
                        p.Id === activeProfile?.Id
                          ? 'bg-neutral-700 text-white'
                          : 'text-neutral-500 hover:text-neutral-300 hover:bg-neutral-800'
                      }`}
                    >
                      <IconBriefcase size={11} />
                      {p.Name}
                    </button>
                    {profiles.length > 1 && p.Id === activeProfile?.Id && (
                      <button
                        onClick={() => handleDeleteProfile(p.Id)}
                        disabled={deleteProfile.isPending}
                        className="ml-0.5 p-1 text-neutral-600 hover:text-red-400 transition-colors opacity-0 group-hover:opacity-100"
                        title="Delete this profile"
                      >
                        <IconTrash size={10} />
                      </button>
                    )}
                  </div>
                ))}

                {/* Create new profile */}
                {creatingProfile ? (
                  <div className="flex items-center gap-1 pl-1">
                    <input
                      autoFocus
                      value={newProfileName}
                      onChange={e => setNewProfileName(e.currentTarget.value)}
                      onKeyDown={e => {
                        if (e.key === 'Enter') handleCreateProfile()
                        if (e.key === 'Escape') { setCreatingProfile(false); setNewProfileName('') }
                      }}
                      placeholder="Profile name"
                      className="bg-neutral-800 border border-neutral-700 rounded-md px-2 py-1 text-xs text-neutral-200 placeholder-neutral-600 outline-none w-28 focus:border-neutral-500"
                    />
                    <button
                      onClick={handleCreateProfile}
                      disabled={createProfile.isPending}
                      className="text-xs px-2 py-1 rounded-md bg-neutral-700 text-neutral-300 hover:bg-neutral-600 transition-colors"
                    >
                      Add
                    </button>
                    <button
                      onClick={() => { setCreatingProfile(false); setNewProfileName('') }}
                      className="text-xs px-2 py-1 rounded-md text-neutral-500 hover:text-neutral-300 transition-colors"
                    >
                      Cancel
                    </button>
                  </div>
                ) : (
                  <button
                    onClick={() => setCreatingProfile(true)}
                    className="p-1.5 text-neutral-500 hover:text-neutral-300 transition-colors"
                    title="New profile"
                  >
                    <IconPlus size={12} />
                  </button>
                )}
              </div>
            </div>
          </div>

          <div className="space-y-3">
            <CVManagement profile={activeProfile} onUpdate={handleProfileUpdate} />
            <UserPreferences profile={activeProfile} onUpdate={handleProfileUpdate} />
            <IgnoredJobsSection />

            {/* AI features */}
            <div className="rounded-xl border border-neutral-800 bg-neutral-900/50 px-4 py-3.5 flex items-center justify-between">
              <div className="flex items-center gap-3">
                <IconSparkles size={16} className="text-neutral-400 flex-shrink-0" />
                <div>
                  <span className="text-sm font-medium text-neutral-200">AI Features</span>
                  <span className="ml-3 text-xs text-neutral-500">Embedding-based job recommendations</span>
                </div>
              </div>
              <Switch
                checked={settings?.GenerateEmbeddings ?? false}
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
