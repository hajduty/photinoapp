'use client'

import React, { useState } from 'react'
import {
  Modal,
  Textarea,
  Text
} from '@mantine/core'
import { sendPhotinoRequest } from '@/app/utils/photino'
import { Settings } from '@/app/types/settings/settings'
import { UpdateSettingsRequest } from '@/app/types/settings/update-settings-request'
import { UpdateSettingsResponse } from '@/app/types/settings/update-settings-response'
import { UpdatePreferencesRequest } from '@/app/types/settings/update-preferences-request'
import { IconX } from '@tabler/icons-react'

interface CVManagementProps {
  settings: Settings | null
  onUpdate: (settings: Settings) => void
}

export default function CVManagement({ settings, onUpdate }: CVManagementProps) {
  const [opened, setOpened] = useState(false)
  const [cvContent, setCvContent] = useState(settings?.UserCV || '')
  const [loading, setLoading] = useState(false)

  const handleSave = async () => {
    try {
      setLoading(true)

      const request: UpdatePreferencesRequest = {
        UserCV: cvContent,
        SelectedTagIds: null,
        YearsOfExperience: null,
        BlockedKeywords: null,
        MatchedKeywords: null,
        AlertOnAllMatchingJobs: null,
        AlertOnHardMatchingJobs: null,
        Location: null,
        MaxJobAgeDays: null
      }

      await sendPhotinoRequest<UpdateSettingsResponse>('settings.updatePreferences', request)

      // Update the parent component with new settings
      if (settings) {
        onUpdate({ ...settings, UserCV: cvContent })
      }

      setOpened(false)
    } catch (err) {
      console.error('Failed to update CV:', err)
    } finally {
      setLoading(false)
    }
  }

  return (
    <>
      <button
        onClick={() => setOpened(true)}
        className="btn-secondary text-sm"
      >
        {settings?.UserCV ? 'Edit CV' : 'Add CV'}
      </button>

      <Modal
        lockScroll={false}
        opened={opened}
        onClose={() => setOpened(false)}
        size="xl"
        centered
        withCloseButton={false}
      >
        <div>
          {/* Header */}
          <div className="flex items-center justify-between mb-4">
            <div>
              <h2 className="text-sm font-medium text-white">Edit CV</h2>
              <p className="text-xs text-neutral-500 mt-0.5">
                Paste your CV contents below, this will be used for job recommendations.
              </p>
            </div>
            <button
              className="p-1 rounded text-neutral-600 hover:text-neutral-300 transition-colors"
              onClick={() => setOpened(false)}
            >
              <IconX></IconX>
            </button>
          </div>

          <div className="border-t border-neutral-800 -mx-[var(--modal-padding,1rem)]" />

          <Textarea
            value={cvContent}
            onChange={(event) => setCvContent(event.currentTarget.value)}
            placeholder="Paste your CV content here..."
            minRows={15}
            maxRows={25}
            autosize
            mb="md"
            classNames={{
              input: 'bg-neutral-800 border-neutral-700 text-neutral-200 placeholder-neutral-500',
              label: 'text-neutral-300'
            }}
            className='mt-4'
          />
          <div className="flex justify-end gap-3 mt-4">
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
              Save CV
            </button>
          </div>
        </div>
      </Modal>
    </>
  )
}
