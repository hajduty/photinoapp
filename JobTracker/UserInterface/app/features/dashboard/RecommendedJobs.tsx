import React, { useState } from 'react';
import { ExtendedPosting } from '../../types/jobs/extended-posting';
import { IconLocation, IconBookmark, IconX } from '@tabler/icons-react';
import { getContrastColor } from '../../utils/getContrastColor';
import JobDetailsModal from '../search/JobDetailsModal';

interface RecommendedJobsProps {
  jobs: ExtendedPosting[] | { Jobs: ExtendedPosting[] };
  bookmarkedJobs?: Set<number>;
  onBookmark: (jobId: number, targetState: boolean) => void;
  onIgnore?: (jobId: number) => void;
  isLoading?: boolean;
}

function JobCardSkeleton() {
  return (
    <div className="relative flex flex-col gap-2 px-3 py-2.5 rounded-xl border border-neutral-800 bg-neutral-900/50 overflow-hidden h-[105px]">
      {/* Shimmer overlay */}
      <div className="absolute inset-0 animate-pulse bg-neutral-800/30 rounded-xl" />

      {/* Exact same structure as real card, all text invisible */}
      <div className="flex items-start justify-between gap-2">
        <h3 className="text-[12.5px] font-medium leading-snug line-clamp-2 flex-1 min-w-0 invisible select-none">
          Job title placeholder that wraps
        </h3>
        <div className="flex items-center gap-0.5 flex-shrink-0 mt-0.5">
          <button className="p-1 invisible"><IconBookmark size={13} /></button>
        </div>
      </div>

      <div className="flex items-center justify-between gap-2">
        <span className="text-[11.5px] invisible select-none">Company name</span>
        <span className="text-[11px] flex-shrink-0 invisible select-none">Mar 00</span>
      </div>

      <div className="flex items-center justify-between gap-2 mt-auto">
        <div className="flex items-center gap-1 min-w-0 overflow-hidden">
          <IconLocation size={11} className="flex-shrink-0 invisible" />
          <span className="text-[11px] invisible select-none">Location</span>
        </div>
        <div className="flex items-center gap-1 flex-shrink-0">
          <span className="text-[9.5px] font-semibold uppercase tracking-wide px-1.5 py-0.5 rounded invisible select-none">REACT</span>
          <span className="text-[9.5px] font-semibold uppercase tracking-wide px-1.5 py-0.5 rounded invisible select-none">TYPE</span>
        </div>
      </div>
    </div>
  );
}

export default function RecommendedJobs({
  jobs,
  bookmarkedJobs,
  onBookmark,
  onIgnore,
  isLoading = false,
}: RecommendedJobsProps) {
  const [selectedJob, setSelectedJob] = useState<ExtendedPosting | null>(null);
  const [modalOpened, setModalOpened] = useState(false);

  const jobList = Array.isArray(jobs) ? jobs : (jobs?.Jobs ?? []);

  return (
    <div>
      <p className="text-[11px] font-medium text-neutral-500 uppercase tracking-widest mb-3 select-none">
        Recommended Jobs
      </p>

      {isLoading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-2.5">
          {Array.from({ length: 15 }).map((_, i) => (
            <JobCardSkeleton key={i} />
          ))}
        </div>
      ) : !jobList.length ? (
        <div className="px-4 py-3 border border-neutral-800 rounded-xl bg-neutral-900/50">
          <p className="text-sm text-neutral-500">No recommended jobs available.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-2.5">
          {jobList.map((job) => {
            const isBookmarked = bookmarkedJobs?.has(job.Posting.Id) ?? job.Posting.Bookmarked;

            return (
              <div
                key={job.Posting.Id}
                onClick={() => { setSelectedJob(job); setModalOpened(true); }}
                className="group flex flex-col gap-2 px-3 py-2.5 rounded-xl border border-neutral-800 bg-neutral-900/50 hover:border-neutral-700 hover:bg-neutral-800/40 transition-all cursor-pointer h-[105px]"
              >
                <div className="flex items-start justify-between gap-2">
                  <h3 className="text-[12.5px] font-medium text-white leading-snug line-clamp-2 flex-1 min-w-0">
                    {job.Posting.Title}
                  </h3>
                  <div className="flex items-center gap-0.5 flex-shrink-0 mt-0.5">
                    {onIgnore && (
                      <button
                        onClick={(e) => { e.stopPropagation(); onIgnore(job.Posting.Id); }}
                        className="p-1 rounded text-neutral-600 hover:text-red-400 opacity-0 group-hover:opacity-100 transition-all"
                        aria-label="Ignore job"
                      >
                        <IconX size={13} />
                      </button>
                    )}
                    <button
                      onClick={(e) => { e.stopPropagation(); onBookmark(job.Posting.Id, !isBookmarked); }}
                      className={`p-1 rounded transition-colors ${isBookmarked ? 'text-amber-400' : 'text-neutral-600 hover:text-neutral-400'}`}
                      aria-label={isBookmarked ? 'Remove bookmark' : 'Bookmark job'}
                    >
                      <IconBookmark size={13} fill={isBookmarked ? 'currentColor' : 'none'} />
                    </button>
                  </div>
                </div>

                <div className="flex items-center justify-between gap-2">
                  <span className="text-[11.5px] text-neutral-400 truncate">{job.Posting.Company}</span>
                  <span className="text-[11px] text-neutral-600 flex-shrink-0">
                    {new Date(job.Posting.PostedDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
                  </span>
                </div>

                <div className="flex items-center justify-between gap-2 mt-auto">
                  <div className="flex items-center gap-1 min-w-0 overflow-hidden">
                    <IconLocation size={11} className="flex-shrink-0 text-neutral-600" />
                    <span className="text-[11px] text-neutral-500 truncate">{job.Posting.Location}</span>
                  </div>
                  <div className="flex items-center gap-1 flex-shrink-0">
                    {job.Tags.slice(0, 2).map((tag) => (
                      <span
                        key={tag.Id}
                        className="text-[9.5px] font-semibold uppercase tracking-wide px-1.5 py-0.5 rounded"
                        style={{ backgroundColor: tag.Color, color: getContrastColor(tag.Color) }}
                      >
                        {tag.Name}
                      </span>
                    ))}
                    {job.Tags.length > 2 && (
                      <span className="text-[10px] text-neutral-500 px-1.5 py-0.5 rounded bg-neutral-800">
                        +{job.Tags.length - 2}
                      </span>
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {selectedJob && (() => {
        const isBookmarked = bookmarkedJobs?.has(selectedJob.Posting.Id) ?? selectedJob.Posting.Bookmarked;
        return (
          <JobDetailsModal
            posting={selectedJob}
            opened={modalOpened}
            onClose={() => setModalOpened(false)}
            onBookmark={() => onBookmark(selectedJob.Posting.Id, !isBookmarked)}
            isBookmarked={isBookmarked}
            bookmarkedJobs={bookmarkedJobs}
          />
        );
      })()}
    </div>
  );
}