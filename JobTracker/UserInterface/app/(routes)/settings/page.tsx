'use client'

import React from 'react'
import {
  Box,
  Title,
  Text,
  Space,
  Flex} from '@mantine/core'
import TagManagement from '../../features/settings/TagManagement'
import ApiManagement from '../../features/settings/ApiManagement'

export default function SettingsPage() {
  return (
    <div className="p-8">
      <div className="max-w-7xl mx-auto py-6">
        <Flex justify="space-between" align="center" mb="md">
          <Box>
            <Title order={2} c="white">SETTINGS</Title>
            <Text c="dimmed">Manage settings and preferences</Text>
          </Box>
        </Flex>

        <TagManagement />
        
        <Space h="xl" />
        
        <ApiManagement />
      </div>
    </div>
  )
}
