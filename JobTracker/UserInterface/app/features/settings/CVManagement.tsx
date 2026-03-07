'use client'

import React, { useState } from 'react'
import {
  Modal,
  Textarea,
  Text} from '@mantine/core'
import { sendPhotinoRequest } from '@/app/utils/photino'
import { Settings } from '@/app/types/settings/settings'
import { UpdateSettingsRequest } from '@/app/types/settings/update-settings-request'
import { UpdateSettingsResponse } from '@/app/types/settings/update-settings-response'

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
      
      const request: UpdateSettingsRequest = {
        DiscordWebhookUrl: settings?.DiscordWebhookUrl ?? '',
        DiscordNotificationsEnabled: settings?.DiscordNotificationsEnabled ?? false,
        GenerateEmbeddings: settings?.GenerateEmbeddings ?? false,
        UserCV: cvContent
      }

      await sendPhotinoRequest<UpdateSettingsResponse>('settings.updateSettings', request)
      
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
        title="CV Content"
        size="xl"
        centered
        classNames={{
          content: 'bg-neutral-900 border border-neutral-800',
          title: 'text-neutral-200',
          close: 'text-neutral-400 hover:text-white'
        }}
      >
        <div>
          <Text className="text-neutral-300 mb-4">
            Enter your CV content below. This will be used for AI-powered job matching and analysis.
          </Text>
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
