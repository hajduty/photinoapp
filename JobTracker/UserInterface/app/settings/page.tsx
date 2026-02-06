'use client'

import React from 'react'
import {
  Box,
  Title,
  Text,
  Container,
  ScrollArea,
  Space,
  Flex,
  rem
} from '@mantine/core'
import { IconSettings } from '@tabler/icons-react'
import TagManagement from './TagManagement'
import ApiManagement from './ApiManagement'

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
