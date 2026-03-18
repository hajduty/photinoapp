'use client'

import React, { useState, useEffect } from 'react'
import {
  Modal,
  TextInput,
  Text,
  MultiSelect,
  NumberInput,
  Switch,
  TagsInput
} from '@mantine/core'
import { sendPhotinoRequest } from '@/app/utils/photino'
import { Settings } from '@/app/types/settings/settings'
import { UpdatePreferencesRequest } from '@/app/types/settings/update-preferences-request'
import { UpdatePreferencesResponse } from '@/app/types/settings/update-preferences-response'
import { Tag } from '@/app/types/tag/tag'
import { useTags } from '@/app/hooks/useTags'
import IgnoredJobsModal from './IgnoredJobsModal'

interface UserPreferencesProps {
  settings: Settings | null
  onUpdate: (settings: Settings) => void
}

export default function UserPreferences({ settings, onUpdate }: UserPreferencesProps) {
  const [opened, setOpened] = useState(false)
  const [ignoredJobsModalOpened, setIgnoredJobsModalOpened] = useState(false)
  const [loading, setLoading] = useState(false)
  const [selectedTags, setSelectedTags] = useState<string[]>([])
  const [yearsOfExperience, setYearsOfExperience] = useState<number | null>(null)
  const [blockedKeywords, setBlockedKeywords] = useState<string[]>([])
  const [matchedKeywords, setMatchedKeywords] = useState<string[]>([])
  const [alertOnAllMatchingJobs, setAlertOnAllMatchingJobs] = useState(false)
  const [alertOnHardMatchingJobs, setAlertOnHardMatchingJobs] = useState(false)
  const [location, setLocation] = useState('')
  const [maxJobAgeDays, setMaxJobAgeDays] = useState<number | null>(null)

  // Use TanStack Query hooks
  const { data: tags = [] } = useTags()

  useEffect(() => {
    if (settings) {
      // Initialize form values from settings
      setYearsOfExperience(settings.YearsOfExperience)
      setAlertOnAllMatchingJobs(settings.AlertOnAllMatchingJobs ?? false)
      setAlertOnHardMatchingJobs(settings.AlertOnHardMatchingJobs ?? false)
      setLocation(settings.Location ?? '')
      setMaxJobAgeDays(settings.MaxJobAgeDays)
      setBlockedKeywords(settings.BlockedKeywords ?? [])
      setMatchedKeywords(settings.MatchedKeywords ?? [])
      
      // Convert selected tags to string array for MultiSelect
      if (settings.SelectedTags && settings.SelectedTags.length > 0) {
        setSelectedTags(settings.SelectedTags.map((tag: Tag) => tag.Id.toString()))
      }
    }
  }, [settings])

  const handleSave = async () => {
    try {
      setLoading(true)
      
      const request: UpdatePreferencesRequest = {
        UserCV: settings?.UserCV ?? null,
        SelectedTagIds: selectedTags.length > 0 ? selectedTags.map(id => parseInt(id)) : null,
        YearsOfExperience: yearsOfExperience,
        BlockedKeywords: blockedKeywords.length > 0 ? blockedKeywords : null,
        MatchedKeywords: matchedKeywords.length > 0 ? matchedKeywords : null,
        AlertOnAllMatchingJobs: alertOnAllMatchingJobs,
        AlertOnHardMatchingJobs: alertOnHardMatchingJobs,
        Location: location || null,
        MaxJobAgeDays: maxJobAgeDays,
      }

      const response = await sendPhotinoRequest<UpdatePreferencesResponse>('settings.updatePreferences', request)
      
      // Update the parent component with new settings
      onUpdate(response.Settings)
      
      setOpened(false)
    } catch (err) {
      console.error('Failed to update preferences:', err)
    } finally {
      setLoading(false)
    }
  }

  const tagOptions = tags.map(tag => ({
    value: tag.Id.toString(),
    label: tag.Name
  }))

  return (
    <>
      <div className="flex gap-2">
        <button
          onClick={() => setOpened(true)}
          className="btn-secondary text-sm"
        >
          Configure Preferences
        </button>
        <button
          onClick={() => setIgnoredJobsModalOpened(true)}
          className="btn-secondary text-sm"
        >
          View Ignored Jobs
        </button>
      </div>

      <Modal
        lockScroll={false}
        opened={opened}
        onClose={() => setOpened(false)}
        title="User Preferences"
        size="xl"
        centered
        classNames={{
          content: 'bg-neutral-900 border border-neutral-800',
          title: 'text-neutral-200',
          close: 'text-neutral-400 hover:text-white'
        }}
      >
        <div className="space-y-6">
          <div>
            <Text className="text-neutral-300 mb-3 font-medium">Basic Information</Text>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <NumberInput
                label="Years of Experience"
                placeholder="Enter years of experience"
                value={yearsOfExperience ?? undefined}
                onChange={(value) => setYearsOfExperience(typeof value === 'number' ? value : null)}
                min={0}
                max={50}
                classNames={{
                  input: 'bg-neutral-800 border-neutral-700 text-neutral-200 placeholder-neutral-500',
                  label: 'text-neutral-300'
                }}
              />
              <TextInput
                label="Preferred Location"
                placeholder="Enter your preferred location"
                value={location}
                onChange={(event) => setLocation(event.currentTarget.value)}
                classNames={{
                  input: 'bg-neutral-800 border-neutral-700 text-neutral-200 placeholder-neutral-500',
                  label: 'text-neutral-300'
                }}
              />
            </div>
          </div>

          <div>
            <Text className="text-neutral-300 mb-3 font-medium">Job Matching</Text>
            <div className="space-y-4">
              <MultiSelect
                label="Preferred Tags"
                placeholder="Select tags you're interested in"
                value={selectedTags}
                onChange={setSelectedTags}
                data={tagOptions}
                searchable
                nothingFoundMessage="No tags found"
                classNames={{
                  input: 'bg-neutral-800 border-neutral-700 text-neutral-200 placeholder-neutral-500',
                  label: 'text-neutral-300',
                  dropdown: 'bg-neutral-800 border-neutral-700',
                  option: 'text-neutral-200 hover:bg-neutral-700'
                }}
              />
              <NumberInput
                label="Maximum Job Age (days)"
                placeholder="Enter maximum age for jobs to show"
                value={maxJobAgeDays ?? undefined}
                onChange={(value) => setMaxJobAgeDays(typeof value === 'number' ? value : null)}
                min={1}
                max={365}
                classNames={{
                  input: 'bg-neutral-800 border-neutral-700 text-neutral-200 placeholder-neutral-500',
                  label: 'text-neutral-300'
                }}
              />
            </div>
          </div>

          <div>
            <Text className="text-neutral-300 mb-3 font-medium">Alert Preferences</Text>
            <div className="space-y-3">
              <Switch
                label="Alert on all matching jobs"
                checked={alertOnAllMatchingJobs}
                onChange={(event) => setAlertOnAllMatchingJobs(event.currentTarget.checked)}
                classNames={{
                  label: 'text-neutral-300',
                  track: 'bg-neutral-700'
                }}
              />
              <Switch
                label="Alert on hard matching jobs"
                checked={alertOnHardMatchingJobs}
                onChange={(event) => setAlertOnHardMatchingJobs(event.currentTarget.checked)}
                classNames={{
                  label: 'text-neutral-300',
                  track: 'bg-neutral-700'
                }}
              />
            </div>
          </div>

          <div>
            <Text className="text-neutral-300 mb-3 font-medium">Keywords</Text>
            <div className="space-y-4">
              <TagsInput
                label="Blocked Keywords"
                placeholder="Add keywords to block"
                value={blockedKeywords}
                onChange={setBlockedKeywords}
                splitChars={[',', ' ', ';']}
                classNames={{
                  input: 'bg-neutral-800 border-neutral-700 text-neutral-200 placeholder-neutral-500',
                  label: 'text-neutral-300'
                }}
              />
              <TagsInput
                label="Matched Keywords"
                placeholder="Add keywords to match"
                value={matchedKeywords}
                onChange={setMatchedKeywords}
                splitChars={[',', ' ', ';']}
                classNames={{
                  input: 'bg-neutral-800 border-neutral-700 text-neutral-200 placeholder-neutral-500',
                  label: 'text-neutral-300'
                }}
              />
            </div>
          </div>

          <div className="flex justify-end gap-3 mt-6">
            <button 
              onClick={() => setOpened(false)}
              disabled={loading}
              className="btn-ghost text-sm"
            >
              Cancel
            </button>
            <button 
              onClick={handleSave}
              disabled={loading}
              className="btn-secondary text-sm"
            >
              Save Preferences
            </button>
          </div>
        </div>
      </Modal>

      <IgnoredJobsModal
        opened={ignoredJobsModalOpened}
        onClose={() => setIgnoredJobsModalOpened(false)}
      />
    </>
  )
}
