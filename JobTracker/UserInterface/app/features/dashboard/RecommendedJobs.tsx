import React, { useState, useRef, useEffect } from 'react';
import { flushSync } from 'react-dom';
import { Modal } from '@mantine/core';
import { ExtendedPosting } from '../../types/jobs/extended-posting';
import { IconLocation, IconBookmark, IconDotsVertical, IconEyeOff, IconBan, IconAlertTriangle, IconX } from '@tabler/icons-react';
import { getContrastColor } from '../../utils/getContrastColor';
import JobDetailsModal from '../search/JobDetailsModal';
import { TagSelectionModal } from '../../components/TagSelectionModal';
import { IgnoreJobRequest, IgnoreReason } from '../../types/jobs/ignore-job-request';

interface RecommendedJobsProps {
  jobs: ExtendedPosting[] | { Jobs: ExtendedPosting[] };
  bookmarkedJobs?: Set<number>;
  onBookmark: (jobId: number, targetState: boolean) => void;
  onIgnore?: (request: IgnoreJobRequest) => void;
  onSoftIgnore?: (jobId: number) => void;
  isLoading?: boolean;
}

function JobCardSkeleton() {
  return (
    <div className="relative flex flex-col gap-2 px-3 py-2.5 rounded-xl border border-neutral-800 bg-neutral-900/50 overflow-hidden h-[105px]">
      <div className="absolute inset-0 animate-pulse bg-neutral-800/30 rounded-xl" />
      <div className="flex items-start justify-between gap-2">
        <h3 className="text-xs font-medium leading-snug line-clamp-2 flex-1 min-w-0 invisible select-none">
          Job title placeholder that wraps
        </h3>
        <div className="flex items-center gap-0.5 flex-shrink-0 mt-0.5">
          <button className="p-1 invisible"><IconBookmark size={13} /></button>
        </div>
      </div>
      <div className="flex items-center justify-between gap-2">
        <span className="text-xs invisible select-none">Company name</span>
        <span className="text-xs flex-shrink-0 invisible select-none">Mar 00</span>
      </div>
      <div className="flex items-center justify-between gap-2 mt-auto">
        <div className="flex items-center gap-1 min-w-0 overflow-hidden">
          <IconLocation size={11} className="flex-shrink-0 invisible" />
          <span className="text-xs invisible select-none">Location</span>
        </div>
        <div className="flex items-center gap-1 flex-shrink-0">
          <span className="text-xs font-semibold uppercase tracking-wide px-1.5 py-0.5 rounded invisible select-none">REACT</span>
          <span className="text-xs font-semibold uppercase tracking-wide px-1.5 py-0.5 rounded invisible select-none">TYPE</span>
        </div>
      </div>
    </div>
  );
}

interface JobOptionsMenuProps {
  jobId: number;
  jobTitle: string;
  jobTags: { Id: number; Name: string; Color: string }[];
  onSoftIgnore?: (jobId: number) => void;
  onConfirmIgnore: (jobId: number, jobTitle: string, jobTags: { Id: number; Name: string; Color: string }[]) => void;
}

function JobOptionsMenu({ jobId, jobTitle, jobTags, onSoftIgnore, onConfirmIgnore }: JobOptionsMenuProps) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [open]);

  return (
    <div ref={ref} className="relative">
      <button
        onClick={(e) => { e.stopPropagation(); setOpen(o => !o); }}
        className="p-1 rounded text-neutral-600 hover:text-neutral-400 opacity-0 group-hover:opacity-100 transition-all"
        aria-label="Job options"
      >
        <IconDotsVertical size={13} />
      </button>

      {open && (
        <div
          className="absolute right-0 top-full mt-1 z-50 min-w-[200px] rounded-lg border border-neutral-700 bg-neutral-900 shadow-xl py-1"
          onClick={(e) => e.stopPropagation()}
        >
          {onSoftIgnore && (
            <button
              onClick={(e) => { e.stopPropagation(); onSoftIgnore(jobId); setOpen(false); }}
              className="w-full flex items-center gap-2.5 px-3 py-2 text-xs text-neutral-300 hover:bg-neutral-800 hover:text-white transition-colors text-left"
            >
              <IconEyeOff size={13} className="text-neutral-500 flex-shrink-0" />
              Hide this job
            </button>
          )}
          <button
            onClick={(e) => { e.stopPropagation(); setOpen(false); onConfirmIgnore(jobId, jobTitle, jobTags); }}
            className="w-full flex items-center gap-2.5 px-3 py-2 text-xs text-neutral-300 hover:bg-neutral-800 hover:text-red-400 transition-colors text-left"
          >
            <IconBan size={13} className="text-neutral-500 flex-shrink-0" />
            Never show similar
          </button>
        </div>
      )}
    </div>
  );
}

const ignoreReasons: { value: IgnoreReason; label: string; description: string }[] = [
  { value: 'tags', label: "Tech stack mismatch", description: "Penalize similar tech stacks in the future" },
  { value: 'experience', label: "Seniority mismatch", description: "Block this seniority level for similar jobs" },
  { value: 'location', label: "Location mismatch", description: "Block this location from showing up again" },
  { value: 'requirements', label: "Requirements mismatch", description: "Just hide this job, no future penalty" },
  { value: 'title', label: "Title mismatch", description: "Just hide this job, no future penalty" },
];

export default function RecommendedJobs({
  jobs,
  bookmarkedJobs,
  onBookmark,
  onIgnore,
  onSoftIgnore,
  isLoading = false,
}: RecommendedJobsProps) {
  const [selectedJob, setSelectedJob] = useState<ExtendedPosting | null>(null);
  const [modalOpened, setModalOpened] = useState(false);
  const [modalIsBookmarked, setModalIsBookmarked] = useState(false);
  const [confirmIgnoreJob, setConfirmIgnoreJob] = useState<{ id: number; title: string; jobTags: { Id: number; Name: string; Color: string }[] } | null>(null);
  const [selectedReason, setSelectedReason] = useState<IgnoreReason | null>('tags');
  const [showTagSelection, setShowTagSelection] = useState(false);
  const [selectedTagIds, setSelectedTagIds] = useState<number[]>([]);

  const openModal = (job: ExtendedPosting) => {
    const currentState = bookmarkedJobs?.has(job.Posting.Id) ?? job.Posting.Bookmarked;
    flushSync(() => {
      setSelectedJob(job);
      setModalIsBookmarked(currentState);
    });
    setModalOpened(true);
  };

  const handleModalBookmark = () => {
    if (!selectedJob) return;
    const next = !modalIsBookmarked;
    setModalIsBookmarked(next);
    onBookmark(selectedJob.Posting.Id, next);
  };

  const handleConfirmIgnore = (jobId: number, jobTitle: string, jobTags: { Id: number; Name: string; Color: string }[]) => {
    setConfirmIgnoreJob({ id: jobId, title: jobTitle, jobTags });
    setSelectedReason('tags');
    setSelectedTagIds([]);
  };

  const handleIgnore = () => {
    if (!confirmIgnoreJob || !selectedReason || !onIgnore) return;

    if (selectedReason === 'tags' && selectedTagIds.length === 0) {
      setShowTagSelection(true);
      return;
    }

    onIgnore({
      JobId: confirmIgnoreJob.id,
      Reason: selectedReason,
      RejectedTags: selectedReason === 'tags' ? selectedTagIds : undefined,
    });

    setConfirmIgnoreJob(null);
    setSelectedReason('tags');
    setSelectedTagIds([]);
  };

  const handleToggleTag = (tagId: number) => {
    setSelectedTagIds(prev =>
      prev.includes(tagId)
        ? prev.filter(id => id !== tagId)
        : [...prev, tagId]
    );
  };

  const handleTagSelectionClose = () => {
    setShowTagSelection(false);
  };

  const handleTagSelectionConfirm = () => {
    if (confirmIgnoreJob && selectedReason && selectedTagIds.length > 0 && onIgnore) {
      onIgnore({
        JobId: confirmIgnoreJob.id,
        Reason: selectedReason,
        RejectedTags: selectedTagIds,
      });
    }

    setConfirmIgnoreJob(null);
    setSelectedReason('tags');
    setSelectedTagIds([]);
    setShowTagSelection(false);
  };

  const jobList = Array.isArray(jobs) ? jobs : (jobs?.Jobs ?? []);

  return (
    <div>
      <p className="text-xs font-medium text-neutral-500 uppercase tracking-widest mb-3 select-none">
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
                onClick={() => openModal(job)}
                className="group flex flex-col gap-2 px-3 py-2.5 rounded-xl border border-neutral-800 bg-neutral-900/50 hover:border-neutral-700 hover:bg-neutral-800/40 transition-all cursor-pointer h-[105px]"
              >
                <div className="flex items-start justify-between gap-2">
                  <h3 className="text-sm font-medium text-white leading-snug line-clamp-2 flex-1 min-w-0">
                    {job.Posting.Title}
                  </h3>
                    <div className="flex items-center gap-0.5 flex-shrink-0 mt-0.5">
                    <JobOptionsMenu
                      jobId={job.Posting.Id}
                      jobTitle={job.Posting.Title}
                      jobTags={job.Tags}
                      onSoftIgnore={onSoftIgnore}
                      onConfirmIgnore={handleConfirmIgnore}
                    />
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
                  <span className="text-xs text-neutral-400 truncate">{job.Posting.Company}</span>
                  <span className="text-xs text-neutral-600 flex-shrink-0">
                    {new Date(job.Posting.PostedDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
                  </span>
                </div>

                <div className="flex items-center justify-between gap-2 mt-auto">
                  <div className="flex items-center gap-1 min-w-0 overflow-hidden">
                    <IconLocation size={11} className="flex-shrink-0 text-neutral-600" />
                    <span className="text-xs text-neutral-500 truncate">{job.Posting.Location}</span>
                  </div>
                  <div className="flex items-center gap-1 flex-shrink-0">
                    {job.Tags.slice(0, 2).map((tag) => (
                      <span
                        key={tag.Id}
                        className="text-xs font-semibold uppercase tracking-wide px-1.5 py-0.5 rounded"
                        style={{ backgroundColor: tag.Color, color: getContrastColor(tag.Color) }}
                      >
                        {tag.Name}
                      </span>
                    ))}
                    {job.Tags.length > 2 && (
                      <span className="text-xs text-neutral-500 px-1.5 py-0.5 rounded bg-neutral-800">
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

      {selectedJob && (
        <JobDetailsModal
          posting={selectedJob}
          opened={modalOpened}
          onClose={() => setModalOpened(false)}
          onBookmark={handleModalBookmark}
          isBookmarked={modalIsBookmarked}
        />
      )}

      <Modal
        opened={confirmIgnoreJob !== null && !showTagSelection}
        onClose={() => setConfirmIgnoreJob(null)}
        title={
          <div className="flex items-center gap-2">
            <IconAlertTriangle size={18} className="text-amber-500" />
            <span>Why didn't this job fit?</span>
          </div>
        }
        centered
        size="sm"
      >
        <div className="space-y-4">
          <p className="text-sm text-neutral-400">
            Select a reason for hiding <span className="text-white font-medium">&quot;{confirmIgnoreJob?.title}&quot;</span>
          </p>

          <div className="space-y-2">
            {ignoreReasons.map((reason) => (
              <label
                key={reason.value}
                className={`flex items-start gap-3 p-3 rounded-lg border cursor-pointer transition-all ${
                  selectedReason === reason.value
                    ? 'border-red-500 bg-red-500/10'
                    : 'border-neutral-700 hover:border-neutral-600 bg-neutral-800/50'
                }`}
              >
                <input
                  type="radio"
                  name="ignore-reason"
                  value={reason.value}
                  checked={selectedReason === reason.value}
                  onChange={() => setSelectedReason(reason.value)}
                  className="mt-0.5 accent-red-500"
                />
                <div>
                  <div className="text-sm text-white font-medium">{reason.label}</div>
                  <div className="text-xs text-neutral-500">{reason.description}</div>
                </div>
              </label>
            ))}
          </div>

          <div className="flex gap-2 justify-end pt-2">
            <button
              onClick={() => setConfirmIgnoreJob(null)}
              className="px-3 py-1.5 text-xs rounded border border-neutral-700 text-neutral-400 hover:bg-neutral-800 transition-colors"
            >
              Cancel
            </button>
            <button
              onClick={handleIgnore}
              className="px-3 py-1.5 text-xs rounded bg-red-600 text-white hover:bg-red-700 transition-colors"
            >
              Ignore Job
            </button>
          </div>
        </div>
      </Modal>

      <TagSelectionModal
        opened={showTagSelection}
        onClose={handleTagSelectionClose}
        onConfirm={handleTagSelectionConfirm}
        tags={confirmIgnoreJob?.jobTags || []}
        selectedTagIds={selectedTagIds}
        onToggleTag={handleToggleTag}
      />
    </div>
  );
}
