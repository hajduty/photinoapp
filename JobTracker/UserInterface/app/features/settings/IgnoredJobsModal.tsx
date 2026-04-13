'use client'

import React, { useState } from 'react'
import { IconEyeOff, IconLocation, IconCalendarTime, IconClock, IconChevronDown, IconChevronUp } from '@tabler/icons-react'
import { useGetIgnoredJobs } from '@/app/hooks/useJobs'
import { useIgnoreJob } from '@/app/hooks/useIgnoreJob'
import { Posting } from '@/app/types/jobs/posting'
import JobDetailsModal from '../search/JobDetailsModal'

export default function IgnoredJobsSection() {
  const [expanded, setExpanded] = useState(false)
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
      <div className="rounded-xl border border-neutral-800 bg-neutral-900/50 overflow-hidden">
        <button
          className="w-full flex items-center justify-between px-4 py-3.5 hover:bg-neutral-800/40 transition-colors text-left"
          onClick={() => setExpanded(e => !e)}
        >
          <div className="flex items-center gap-3">
            <IconEyeOff size={16} className="text-neutral-400 flex-shrink-0" />
            <span className="text-sm font-medium text-neutral-200">Ignored Jobs</span>
            {!isLoading && (
              <span className="text-xs text-neutral-500">
                {jobs.length === 0 ? 'None' : `${jobs.length} job${jobs.length === 1 ? '' : 's'}`}
              </span>
            )}
          </div>
          {expanded
            ? <IconChevronUp size={15} className="text-neutral-500" />
            : <IconChevronDown size={15} className="text-neutral-500" />
          }
        </button>

        {expanded && (
          <div className="border-t border-neutral-800">
            <p className="text-xs text-neutral-500 px-4 pt-3 pb-2">
              Click "Show again" to restore a job to your search results.
            </p>
            <div className="h-[380px] overflow-y-auto custom-scrollbar px-4 pb-4">
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
                  <IconEyeOff size={32} className="text-neutral-700" />
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
                      {job.CompanyImage && (
                        <img
                          src={job.CompanyImage}
                          alt={`${job.Company} logo`}
                          className="w-8 h-8 object-contain p-1 rounded-sm bg-white flex-shrink-0 mt-0.5"
                        />
                      )}
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
          </div>
        )}
      </div>

      <JobDetailsModal
        posting={selectedJob ? { Posting: selectedJob, Tags: [] } : undefined}
        opened={jobDetailsModalOpened}
        onClose={() => setJobDetailsModalOpened(false)}
      />
    </>
  )
}
