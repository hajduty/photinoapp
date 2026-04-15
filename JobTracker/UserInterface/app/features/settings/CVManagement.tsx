'use client'

import React, { useState } from 'react'
import { Textarea } from '@mantine/core'
import { IconChevronDown, IconChevronUp, IconFileText } from '@tabler/icons-react'
import { useUpdatePreferences } from '@/app/hooks/useSettings'
import { JobProfile } from '@/app/types/settings/job-profile'

interface CVManagementProps {
  profile: JobProfile | null
  onUpdate: (profile: JobProfile) => void
}

export default function CVManagement({ profile, onUpdate }: CVManagementProps) {
  const [expanded, setExpanded] = useState(false)
  const [cvContent, setCvContent] = useState(profile?.UserCV || '')

  const updatePreferences = useUpdatePreferences()

  const handleSave = async () => {
    try {
      const response = await updatePreferences.mutateAsync({
        UserCV: cvContent,
        SelectedTagIds: null,
        YearsOfExperience: null,
        BlockedKeywords: null,
        MatchedKeywords: null,
        AlertOnAllMatchingJobs: null,
        AlertOnHardMatchingJobs: null,
        Locations: null,
        MaxJobAgeDays: null,
      })
      onUpdate(response.Profile)
    } catch (err) {
      console.error('Failed to update CV:', err)
    }
  }

  const wordCount = cvContent.trim() ? cvContent.trim().split(/\s+/).length : 0

  return (
    <div className="rounded-xl border border-neutral-800 bg-neutral-900/50 overflow-hidden">
      <button
        className="w-full flex items-center justify-between px-4 py-3.5 hover:bg-neutral-800/40 transition-colors text-left"
        onClick={() => setExpanded(e => !e)}
      >
        <div className="flex items-center gap-3">
          <IconFileText size={16} className="text-neutral-400 flex-shrink-0" />
          <span className="text-sm font-medium text-neutral-200">Your CV</span>
          <span className="text-xs text-neutral-500">
            {profile?.UserCV ? `${wordCount} words` : 'Not added'}
          </span>
        </div>
        {expanded
          ? <IconChevronUp size={15} className="text-neutral-500" />
          : <IconChevronDown size={15} className="text-neutral-500" />
        }
      </button>

      {expanded && (
        <div className="px-4 pb-4 border-t border-neutral-800">
          <p className="text-xs text-neutral-500 mt-3 mb-3">
            Paste your CV below — it will be used to generate personalized job recommendations.
          </p>
          <Textarea
            value={cvContent}
            onChange={(e) => setCvContent(e.currentTarget.value)}
            placeholder="Paste your CV content here..."
            minRows={12}
            maxRows={22}
            autosize
            classNames={{
              input: 'bg-neutral-800 border-neutral-700 text-neutral-200 placeholder-neutral-500 text-sm',
            }}
          />
          <div className="flex justify-end mt-3">
            <button onClick={handleSave} disabled={updatePreferences.isPending} className="btn-secondary text-sm">
              {updatePreferences.isPending ? 'Saving...' : 'Save CV'}
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
