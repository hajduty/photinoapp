'use client'

import React from 'react'
import { useRouter, usePathname } from 'next/navigation'
import { Stack, Divider, Box, Text } from '@mantine/core'
import { IconHome, IconSearch, IconBookmark, IconFileText, IconSettings, IconBook2, IconFileUpload } from '@tabler/icons-react'
import Notifications from '../features/notifications/Notifications'

interface SidebarProps {
  onNavigate?: () => void;
}

export default function Sidebar({ onNavigate }: SidebarProps) {
  const router = useRouter()
  const pathname = usePathname()

  const handleNavigate = (path: string) => {
    router.push(path)
    onNavigate?.()
  }

  const isActive = (path: string) => pathname === path

  return (
    <div className="h-full flex flex-col bg-neutral-950/20 border-r border-neutral-800 md:w-[220px]">
      {/* Header */}
      <Box className="p-4 border-b border-neutral-800">
        <div className="flex items-center justify-between">
          <Text size="lg" fw={700} c="white">JOBTRACKER</Text>
          <Notifications />
        </div>
      </Box>

      {/* Navigation */}
      <Stack gap={1} p="xs" className="flex-1">
        <div className="sidebar-section">
          <span className="sidebar-section-title">Overview</span>
          <button
            onClick={() => handleNavigate('/')}
            className={`sidebar-nav-item ${isActive('/') ? 'active' : ''}`}
          >
            <IconHome size={18} />
            <span>Dashboard</span>
          </button>
        </div>

        <Divider my="xs" color="dark.5" />

        <div className="sidebar-section">
          <span className="sidebar-section-title">Jobs</span>
          <button
            onClick={() => handleNavigate('/search')}
            className={`sidebar-nav-item ${isActive('/search') ? 'active' : ''}`}
          >
            <IconSearch size={16} />
            <span>Search</span>
          </button>

          {/*           <button 
            onClick={() => handleNavigate('/semantic-search')} 
            className={`sidebar-nav-item ${isActive('/semantic-search') ? 'active' : ''}`}
          >
            <IconSearch size={16} />
            <span>Semantic Search</span>
          </button> */}

          <button
            onClick={() => handleNavigate('/bookmarks')}
            className={`sidebar-nav-item ${isActive('/bookmarks') ? 'active' : ''}`}
          >
            <IconBookmark size={16} />
            <span>Saved</span>
          </button>

          <button
            onClick={() => handleNavigate('/applied')}
            className={`sidebar-nav-item ${isActive('/applied') ? 'active' : ''}`}
          >
            <IconFileUpload size={16} />
            <span>Applied</span>
          </button>

          <button
            onClick={() => handleNavigate('/tracked-searches')}
            className={`sidebar-nav-item ${isActive('/tracked-searches') ? 'active' : ''}`}
          >
            <IconFileText size={16} />
            <span>Tracked</span>
          </button>
        </div>

{/*         <div className="sidebar-section">
          <span className="sidebar-section-title">Tools</span>
        </div> */}

        {/* Spacer to push Config to bottom */}
        <div className="flex-1" />

        <Divider my="xs" color="dark.5" />

        <div className="sidebar-section">
          <span className="sidebar-section-title">System</span>
          <button
            onClick={() => handleNavigate('/settings')}
            className={`sidebar-nav-item ${isActive('/settings') ? 'active' : ''}`}
          >
            <IconSettings size={18} />
            <span>Settings</span>
          </button>
        </div>
      </Stack>
    </div>
  )
}
