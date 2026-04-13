'use client'

import React, { useState, useEffect } from 'react'
import {
  TextInput,
  MultiSelect,
  NumberInput,
  Switch,
  TagsInput,
  Badge,
  ActionIcon,
  Group,
} from '@mantine/core'
import { IconX } from '@tabler/icons-react'
import { sendPhotinoRequest } from '@/app/utils/photino'
import { Settings } from '@/app/types/settings/settings'
import { UpdatePreferencesRequest } from '@/app/types/settings/update-preferences-request'
import { UpdatePreferencesResponse } from '@/app/types/settings/update-preferences-response'
import { Tag } from '@/app/types/tag/tag'
import { useTags } from '@/app/hooks/useTags'
import { useRejectedTechKeywords } from '@/app/hooks/useRejectedTechKeywords'

interface UserPreferencesProps {
  settings: Settings | null
  onUpdate: (settings: Settings) => void
}

const inputCls = {
  input: 'bg-neutral-800 border-neutral-700 text-neutral-200 placeholder-neutral-500',
  dropdown: 'bg-neutral-800 border-neutral-700',
  option: 'text-neutral-200 hover:bg-neutral-700',
}

function FieldLabel({ children }: { children: React.ReactNode }) {
  return <p className="text-xs text-neutral-500 mb-1.5">{children}</p>
}

function SectionLabel({ children }: { children: React.ReactNode }) {
  return (
    <p className="text-xs font-semibold text-neutral-500 uppercase tracking-widest mb-3">
      {children}
    </p>
  )
}

export default function UserPreferences({ settings, onUpdate }: UserPreferencesProps) {
  const [loading, setLoading] = useState(false)
  const [selectedTags, setSelectedTags] = useState<string[]>([])
  const [yearsOfExperience, setYearsOfExperience] = useState<number | null>(null)
  const [blockedKeywords, setBlockedKeywords] = useState<string[]>([])
  const [matchedKeywords, setMatchedKeywords] = useState<string[]>([])
  const [alertOnAllMatchingJobs, setAlertOnAllMatchingJobs] = useState(false)
  const [alertOnHardMatchingJobs, setAlertOnHardMatchingJobs] = useState(false)
  const [location, setLocation] = useState('')
  const [maxJobAgeDays, setMaxJobAgeDays] = useState<number | null>(null)

  const { data: tags = [] } = useTags()
  const { rejectedKeywords, isLoadingRejectedKeywords, removeRejectedKeyword } = useRejectedTechKeywords()

  useEffect(() => {
    if (settings) {
      setYearsOfExperience(settings.YearsOfExperience)
      setAlertOnAllMatchingJobs(settings.AlertOnAllMatchingJobs ?? false)
      setAlertOnHardMatchingJobs(settings.AlertOnHardMatchingJobs ?? false)
      setLocation(settings.Location ?? '')
      setMaxJobAgeDays(settings.MaxJobAgeDays)
      setBlockedKeywords(settings.BlockedKeywords ?? [])
      setMatchedKeywords(settings.MatchedKeywords ?? [])
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
        SelectedTagIds: selectedTags.map(id => parseInt(id)),
        YearsOfExperience: yearsOfExperience,
        BlockedKeywords: blockedKeywords,
        MatchedKeywords: matchedKeywords,
        AlertOnAllMatchingJobs: alertOnAllMatchingJobs,
        AlertOnHardMatchingJobs: alertOnHardMatchingJobs,
        Location: location || null,
        MaxJobAgeDays: maxJobAgeDays,
      }
      const response = await sendPhotinoRequest<UpdatePreferencesResponse>('settings.updatePreferences', request)
      onUpdate(response.Settings)
    } catch (err) {
      console.error('Failed to update preferences:', err)
    } finally {
      setLoading(false)
    }
  }

  const tagOptions = tags.map(tag => ({
    value: tag?.Id?.toString() ?? '',
    label: tag?.Name ?? '',
  }))

  return (
    <div className="rounded-xl border border-neutral-800 bg-neutral-900/50 divide-y divide-neutral-800">

      {/* Basic Information */}
      <div className="px-4 py-4">
        <SectionLabel>Basic Information</SectionLabel>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <FieldLabel>Years of Experience</FieldLabel>
            <NumberInput
              placeholder="e.g. 3"
              value={yearsOfExperience ?? undefined}
              onChange={(value) => setYearsOfExperience(typeof value === 'number' ? value : null)}
              min={0}
              max={50}
              classNames={inputCls}
            />
          </div>
          <div>
            <FieldLabel>Preferred Location</FieldLabel>
            <TextInput
              placeholder="e.g. Stockholm"
              value={location}
              onChange={(e) => setLocation(e.currentTarget.value)}
              classNames={inputCls}
            />
          </div>
        </div>
      </div>

      {/* Job Matching */}
      <div className="px-4 py-4 space-y-4">
        <SectionLabel>Job Matching</SectionLabel>
        <div>
          <FieldLabel>Preferred Tags</FieldLabel>
          <MultiSelect
            placeholder="Select tags you're interested in"
            value={selectedTags}
            onChange={setSelectedTags}
            data={tagOptions}
            searchable
            nothingFoundMessage="No tags found"
            classNames={inputCls}
          />
        </div>
        <div>
          <FieldLabel>Maximum Job Age (days)</FieldLabel>
          <NumberInput
            placeholder="e.g. 30"
            value={maxJobAgeDays ?? undefined}
            onChange={(value) => setMaxJobAgeDays(typeof value === 'number' ? value : null)}
            min={1}
            max={365}
            classNames={inputCls}
          />
        </div>
      </div>

      {/* Keywords */}
      <div className="px-4 py-4 space-y-4">
        <SectionLabel>Keywords</SectionLabel>
        <div>
          <FieldLabel>Blocked Keywords</FieldLabel>
          <TagsInput
            placeholder="Add keywords to block (space, comma, or semicolon)"
            value={blockedKeywords}
            onChange={setBlockedKeywords}
            splitChars={[',', ' ', ';']}
            classNames={inputCls}
          />
        </div>
        <div>
          <FieldLabel>Matched Keywords</FieldLabel>
          <TagsInput
            placeholder="Add keywords to match (space, comma, or semicolon)"
            value={matchedKeywords}
            onChange={setMatchedKeywords}
            splitChars={[',', ' ', ';']}
            classNames={inputCls}
          />
        </div>
      </div>

      {/* Penalized Tags */}
      <div className="px-4 py-4">
        <SectionLabel>Penalized Tech Tags</SectionLabel>
        <p className="text-xs text-neutral-600 mb-3">
          These tags reduce a job's score, pushing them lower in recommendations.
        </p>
        {isLoadingRejectedKeywords ? (
          <p className="text-xs text-neutral-500">Loading...</p>
        ) : rejectedKeywords.length === 0 ? (
          <p className="text-xs text-neutral-600">No penalized tags.</p>
        ) : (
          <Group gap="xs">
            {rejectedKeywords.map(tag => (
              <Badge
                key={tag.Id}
                size="sm"
                variant="light"
                color="red"
                rightSection={
                  <ActionIcon
                    size="xs"
                    color="red"
                    radius="xl"
                    variant="transparent"
                    onClick={() => removeRejectedKeyword(tag.Id)}
                  >
                    <IconX size={10} />
                  </ActionIcon>
                }
              >
                {tag.Name}
              </Badge>
            ))}
          </Group>
        )}
      </div>

      {/* Alerts + Save */}
      <div className="px-4 py-4">
        <SectionLabel>Alerts</SectionLabel>
        <div className="space-y-3 mb-5">
          <Switch
            label="Alert on all matching jobs"
            checked={alertOnAllMatchingJobs}
            onChange={(e) => setAlertOnAllMatchingJobs(e.currentTarget.checked)}
            size="sm"
            classNames={{ label: 'text-neutral-300', track: 'bg-neutral-700' }}
          />
          <Switch
            label="Alert on hard matching jobs"
            checked={alertOnHardMatchingJobs}
            onChange={(e) => setAlertOnHardMatchingJobs(e.currentTarget.checked)}
            size="sm"
            classNames={{ label: 'text-neutral-300', track: 'bg-neutral-700' }}
          />
        </div>
        <div className="flex justify-end">
          <button onClick={handleSave} disabled={loading} className="btn-secondary text-sm">
            {loading ? 'Saving...' : 'Save Preferences'}
          </button>
        </div>
      </div>

    </div>
  )
}
