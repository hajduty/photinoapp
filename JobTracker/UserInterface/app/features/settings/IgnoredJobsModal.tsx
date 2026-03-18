'use client'

import React, { useState } from 'react'
import { IconEyeOff, IconLocation, IconCalendarTime, IconClock, IconX } from '@tabler/icons-react'
import { useGetIgnoredJobs } from '@/app/hooks/useJobs'
import { useIgnoreJob } from '@/app/hooks/useIgnoreJob'
import { Posting } from '@/app/types/jobs/posting'
import JobDetailsModal from '../search/JobDetailsModal'
import { CustomModal } from '@/app/components/CustomModal'
import { Modal } from '@mantine/core'
import { headers } from 'next/headers'

interface IgnoredJobsModalProps {
  opened: boolean
  onClose: () => void
}

export default function IgnoredJobsModal({ opened, onClose }: IgnoredJobsModalProps) {
  const [selectedJob, setSelectedJob] = useState<Posting | null>(null)
  const [jobDetailsModalOpened, setJobDetailsModalOpened] = useState(false)

  const { data: ignoredJobsResponse, isLoading, error } = useGetIgnoredJobs()
  const unignoreJobMutation = useIgnoreJob()

  const handleUnignore = async (e: React.MouseEvent, jobId: number) => {
    e.stopPropagation()
    try {
      await unignoreJobMutation.mutateAsync({ JobId: jobId })
    } catch (err) {
      console.error('Failed to un-ignore job:', err)
    }
  }

  const jobs = ignoredJobsResponse?.Jobs ?? []

  return (
    <>
      <Modal
        opened={opened}
        onClose={onClose}
        withCloseButton={false}
        size="xl"
        centered
        lockScroll={false}
        classNames={{
          content: 'bg-neutral-900 border border-neutral-800',
          title: 'text-neutral-200',
          close: 'text-neutral-400 hover:text-white'
        }}
      >
        {/* Header */}
        <div className="flex items-center justify-between mb-4">
          <div>
            <h2 className="text-sm font-medium text-white">Ignored Jobs</h2>
            <p className="text-xs text-neutral-500 mt-0.5">
              Click "Show again" to restore a job to your search results.
            </p>
          </div>
          <button
            onClick={onClose}
            className="p-1 rounded text-neutral-600 hover:text-neutral-300 transition-colors"
          >
            <IconX></IconX>
          </button>
        </div>

        <div className="border-t border-neutral-800 -mx-[var(--modal-padding,1rem)]" />

        {/* Content */}
        <div className="mt-4 h-[480px] overflow-y-auto custom-scrollbar -mr-2 pr-2">
          {isLoading ? (
            <div className="flex items-center justify-center h-full">
              <div className="w-5 h-5 rounded-full border-2 border-neutral-700 border-t-neutral-400 animate-spin" />
            </div>
          ) : error ? (
            <div className="flex items-center justify-center h-full">
              <p className="text-sm text-red-400">Failed to load ignored jobs.</p>
            </div>
          ) : jobs.length === 0 ? (
            <div className="flex flex-col items-center justify-center h-full gap-3">
              <IconEyeOff size={36} className="text-neutral-700" />
              <p className="text-sm text-neutral-500">No ignored jobs</p>
            </div>
          ) : (
            <div className="space-y-2">
              {jobs.map((job) => (
                <div
                  key={job.Id}
                  onClick={() => { setSelectedJob(job); setJobDetailsModalOpened(true) }}
                  className="group flex items-start gap-3 px-3 py-3 rounded-xl border border-neutral-800 bg-neutral-800/50 hover:border-neutral-700 hover:bg-neutral-800/80 transition-all cursor-pointer"
                >
                  {/* Logo */}
                  {job.CompanyImage && (
                    <img
                      src={job.CompanyImage}
                      alt={`${job.Company} logo`}
                      className="w-8 h-8 object-contain p-1 rounded-sm bg-white flex-shrink-0 mt-0.5"
                    />
                  )}

                  {/* Body */}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-start justify-between gap-3">
                      <div className="min-w-0">
                        <h3 className="text-sm font-medium text-white truncate">{job.Title}</h3>
                        <p className="text-xs text-neutral-200 mt-0.5">{job.Company}</p>
                      </div>
                      <button
                        onClick={(e) => handleUnignore(e, job.Id)}
                        disabled={unignoreJobMutation.isPending}
                        className="flex-shrink-0 text-xs font-medium px-2.5 py-1 rounded-lg border border-neutral-700 text-neutral-200 hover:text-white hover:border-neutral-500 transition-all disabled:opacity-50"
                      >
                        Show again
                      </button>
                    </div>

                    <div className="flex flex-wrap items-center gap-x-4 gap-y-1 mt-2 text-xs text-neutral-400">
                      <span className="flex items-center gap-1">
                        <IconLocation size={11} />
                        {job.Location}
                      </span>
                      <span className="flex items-center gap-1">
                        <IconCalendarTime size={11} />
                        {new Date(job.PostedDate).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })}
                      </span>
                      <span className="flex items-center gap-1">
                        <IconClock size={11} />
                        Last app: {new Date(job.LastApplicationDate).toLocaleDateString()}
                      </span>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="border-t border-neutral-800 -mx-[var(--modal-padding,1rem)] mt-4" />
        <div className="flex justify-end mt-4">
          <button
            onClick={onClose}
            className="text-xs font-medium px-3 py-1.5 rounded-lg border border-neutral-700 text-neutral-400 hover:text-white hover:border-neutral-500 transition-all"
          >
            Close
          </button>
        </div>
      </Modal>

      <JobDetailsModal
        posting={selectedJob ? { Posting: selectedJob, Tags: [] } : undefined}
        opened={jobDetailsModalOpened}
        onClose={() => setJobDetailsModalOpened(false)}
      />
    </>
  )
}