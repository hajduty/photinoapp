'use client'

import React from 'react'
import { useRouter } from 'next/navigation'
import { IconSettings, IconHome, IconSearch, IconBellPlus, IconBrain } from '@tabler/icons-react'
import Notifications from '../features/notifications/Notifications'

interface SidebarProps {
  onNavigate?: () => void;
}

export default function Sidebar({ onNavigate }: SidebarProps) {
  const router = useRouter()

  const handleNavigate = (path: string) => {
    router.push(path)
    onNavigate?.()
  }

  return (
    <div className="min-w-48 bg-neutral-950 text-white h-full flex flex-col">
      {/* Header */}
      <div className="p-6 border-b border-neutral-700">
        <div className="flex items-center justify-between gap-2">
          <div>
            <h2 className="text-lg font-semibold text-white">JOBTRACKER</h2>
          </div>
          <Notifications />
        </div>
      </div>

      {/* Navigation */}
      <nav className="p-4 space-y-2 flex-1">
        <ul className="space-y-1">
          <li>
            <button onClick={() => handleNavigate('/')} className="nav-item w-full flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium hover:bg-neutral-800 transition-colors">
              <IconHome size={20} />
              <span>Dashboard</span>
            </button>
          </li>
          <li>
            <button onClick={() => handleNavigate('/search')} className="nav-item w-full flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium hover:bg-neutral-800 transition-colors">
              <IconSearch size={20} />
              <span>Job Search</span>
            </button>
          </li>
          <li>
            <button onClick={() => handleNavigate('/semantic-search')} className="nav-item w-full flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium hover:bg-neutral-800 transition-colors">
              <IconBrain size={20} />
              <span>AI Search</span>
            </button>
          </li>
          <li>
            <button onClick={() => handleNavigate('/tracked-searches')} className="nav-item w-full flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium hover:bg-neutral-800 transition-colors">
              <IconBellPlus size={20} />
              <span>Trackers</span>
            </button>
          </li>
          <li>
            <button onClick={() => handleNavigate('/settings')} className="nav-item w-full flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium hover:bg-neutral-800 transition-colors">
              <IconSettings size={20} />
              <span>Settings</span>
            </button>
          </li>
        </ul>
      </nav>
    </div>
  )
}
