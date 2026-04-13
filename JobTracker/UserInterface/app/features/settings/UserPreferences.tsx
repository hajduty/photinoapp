'use client'

import React, { useState, useEffect, useRef } from 'react'
import {
  TextInput,
  MultiSelect,
  NumberInput,
  Switch,
  Badge,
  ActionIcon,
  Group,
} from '@mantine/core'
import { IconX } from '@tabler/icons-react'
import { sendPhotinoRequest } from '@/app/utils/photino'
import { Settings } from '@/app/types/settings/settings'
import { UpdatePreferencesRequest } from '@/app/types/settings/update-preferences-request'
import { UpdatePreferencesResponse } from '@/app/types/settings/update-preferences-response'
import { KeywordRule, KeywordScope } from '@/app/types/settings/keyword-rule'
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

const SCOPES: KeywordScope[] = ['Both', 'TitleOnly', 'DescriptionOnly']
const SCOPE_LABEL: Record<KeywordScope, string> = {
  Both: 'Both',
  TitleOnly: 'Title',
  DescriptionOnly: 'Desc',
}

function SectionLabel({ children }: { children: React.ReactNode }) {
  return (
    <p className="text-xs font-semibold text-neutral-500 uppercase tracking-widest mb-3">
      {children}
    </p>
  )
}

function FieldLabel({ children }: { children: React.ReactNode }) {
  return <p className="text-xs text-neutral-500 mb-1.5">{children}</p>
}

function ScopedTagInput({
  value,
  onChange,
  placeholder,
}: {
  value: KeywordRule[]
  onChange: (rules: KeywordRule[]) => void
  placeholder: string
}) {
  const [inputValue, setInputValue] = useState('')
  const [scope, setScope] = useState<KeywordScope>('Both')
  const [inputWidth, setInputWidth] = useState(32)
  const inputRef = useRef<HTMLInputElement>(null)
  const sizerRef = useRef<HTMLSpanElement>(null)

  // Measure the sizer span after every keystroke for pixel-accurate input width
  useEffect(() => {
    if (sizerRef.current) {
      setInputWidth(sizerRef.current.offsetWidth + 1)
    }
  }, [inputValue, value.length, placeholder])

  const cycleScope = () => {
    setScope(s => SCOPES[(SCOPES.indexOf(s) + 1) % SCOPES.length])
  }

  const commit = () => {
    const trimmed = inputValue.trim()
    if (!trimmed) return
    if (!value.some(r => r.Keyword.toLowerCase() === trimmed.toLowerCase())) {
      onChange([...value, { Keyword: trimmed, Scope: scope }])
    }
    setInputValue('')
    setScope('Both')
  }

  const remove = (index: number) => onChange(value.filter((_, i) => i !== index))

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Tab') {
      e.preventDefault()
      cycleScope()
    } else if (e.key === 'Enter' || e.key === ',') {
      e.preventDefault()
      commit()
    } else if (e.key === 'Backspace' && !inputValue && value.length > 0) {
      remove(value.length - 1)
    }
  }

  const sizerText = inputValue || (value.length === 0 ? placeholder : ' ')

  return (
    <div
      onClick={() => inputRef.current?.focus()}
      className="bg-neutral-800 border border-neutral-700 rounded-lg px-3 py-2 flex flex-wrap gap-1.5 min-h-[38px] cursor-text focus-within:border-neutral-500 transition-colors"
    >
      {/* Off-screen sizer — same font as input, measures actual rendered text width */}
      <span
        ref={sizerRef}
        className="text-xs whitespace-pre"
        style={{ position: 'fixed', visibility: 'hidden', pointerEvents: 'none', top: 0, left: 0 }}
        aria-hidden
      >
        {sizerText}
      </span>

      {value.map((rule, i) => (
        <span
          key={i}
          className="inline-flex items-center gap-1 bg-neutral-700/80 border border-neutral-600 rounded-md px-2 py-0.5 text-xs text-neutral-200"
        >
          <span className="text-neutral-500 text-[10px] leading-none">{SCOPE_LABEL[rule.Scope]}</span>
          {rule.Keyword}
          <button
            onClick={(e) => { e.stopPropagation(); remove(i) }}
            className="text-neutral-500 hover:text-neutral-300 ml-0.5 transition-colors"
          >
            <IconX size={10} />
          </button>
        </span>
      ))}

      <div className="flex items-center gap-px">
        <input
          ref={inputRef}
          value={inputValue}
          onChange={e => setInputValue(e.currentTarget.value)}
          onKeyDown={handleKeyDown}
          onBlur={commit}
          placeholder={value.length === 0 ? placeholder : ''}
          className="bg-transparent text-xs text-neutral-200 placeholder-neutral-600 outline-none p-0"
          style={{ width: inputWidth }}
        />
        {inputValue.trim() && (
          <button
            onMouseDown={e => { e.preventDefault(); cycleScope() }}
            className="flex-shrink-0 text-[10px] font-semibold px-1.5 py-0.5 rounded bg-neutral-700 text-neutral-300 hover:bg-neutral-600 transition-colors select-none"
          >
            {SCOPE_LABEL[scope]}
          </button>
        )}
      </div>
    </div>
  )
}

export default function UserPreferences({ settings, onUpdate }: UserPreferencesProps) {
  const [loading, setLoading] = useState(false)
  const [selectedTags, setSelectedTags] = useState<string[]>([])
  const [yearsOfExperience, setYearsOfExperience] = useState<number | null>(null)
  const [blockedKeywords, setBlockedKeywords] = useState<KeywordRule[]>([])
  const [matchedKeywords, setMatchedKeywords] = useState<KeywordRule[]>([])
  const [alertOnAllMatchingJobs, setAlertOnAllMatchingJobs] = useState(false)
  const [alertOnHardMatchingJobs, setAlertOnHardMatchingJobs] = useState(false)
  const [location, setLocation] = useState('')
  const [maxJobAgeDays, setMaxJobAgeDays] = useState<number | null>(null)

  const { data: tags = [] } = useTags()
  const { rejectedKeywords, isLoadingRejectedKeywords, removeRejectedKeyword, updateRejectedTagScope } = useRejectedTechKeywords()

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
        <p className="text-xs text-neutral-600 -mt-2">
          Type a keyword, press <kbd className="px-1 py-0.5 rounded bg-neutral-700 text-neutral-400 text-[10px] font-mono">Tab</kbd> to cycle scope, <kbd className="px-1 py-0.5 rounded bg-neutral-700 text-neutral-400 text-[10px] font-mono">Enter</kbd> to add.
        </p>
        <div>
          <FieldLabel>Blocked Keywords</FieldLabel>
          <ScopedTagInput
            value={blockedKeywords}
            onChange={setBlockedKeywords}
            placeholder="e.g. senior, lead…"
          />
        </div>
        <div>
          <FieldLabel>Matched Keywords</FieldLabel>
          <ScopedTagInput
            value={matchedKeywords}
            onChange={setMatchedKeywords}
            placeholder="e.g. junior, react…"
          />
        </div>
      </div>

      {/* Penalized Tags */}
      <div className="px-4 py-4">
        <SectionLabel>Penalized Tech Tags</SectionLabel>
        <p className="text-xs text-neutral-600 mb-3">
          These tags reduce a job's score. Click the scope pill to change where they're checked.
        </p>
        {isLoadingRejectedKeywords ? (
          <p className="text-xs text-neutral-500">Loading...</p>
        ) : rejectedKeywords.length === 0 ? (
          <p className="text-xs text-neutral-600">No penalized tags. Ignore a job with reason "Tags" to add some.</p>
        ) : (
          <Group gap="xs">
            {rejectedKeywords.map(tag => (
              <Badge
                key={tag.TagId}
                size="lg"
                variant="light"
                color="red"
                leftSection={
                  <button
                    onClick={() => updateRejectedTagScope(tag.TagId, SCOPES[(SCOPES.indexOf(tag.Scope) + 1) % SCOPES.length])}
                    className="text-[10px] font-semibold text-red-400/70 hover:text-red-300 transition-colors leading-none"
                    title="Click to cycle scope"
                  >
                    {SCOPE_LABEL[tag.Scope]}
                  </button>
                }
                rightSection={
                  <ActionIcon
                    size="xs"
                    color="red"
                    radius="xl"
                    variant="transparent"
                    onClick={() => removeRejectedKeyword(tag.TagId)}
                  >
                    <IconX size={10} />
                  </ActionIcon>
                }
              >
                {tag.TagName}
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
            label="Alert on highly matching jobs"
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
