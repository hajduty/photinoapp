'use client';

import { useState, useEffect } from 'react';
import { Modal, Tooltip } from '@mantine/core';
import JobPostingPopup from './features/search/JobPostingPopup';
import { CustomHeatmap } from './features/dashboard/Heatmap';
import RecommendedJobs from './features/dashboard/RecommendedJobs';
import { IconArrowWaveLeftUp, IconX } from '@tabler/icons-react';
import { HeatmapJobData } from './types/dashboard/get-heatmapdate-response';
import { useDashboardData, useHeatmapData, useHeatmapDateData } from './hooks/useDashboard';
import { useMatchingJobs, useBookmarkJob } from './hooks/useJobs';
import { useIgnoreJob, useSoftIgnoreJob } from './hooks/useIgnoreJob';

export default function Dashboard() {
  const { data: matchingJobs, error: jobsError, isLoading } = useMatchingJobs();
  const { mutate: ignoreJob } = useIgnoreJob();
  const bookmarkMutation = useBookmarkJob();
  
  const { mutate: softIgnoreJob } = useSoftIgnoreJob();
  const { data: dashboardData } = useDashboardData();
  const { data: heatmapData } = useHeatmapData();

  const [selectedDate, setSelectedDate] = useState<string | null>(null);
  const [modalOpened, setModalOpened] = useState(false);
  const [selectedJobs, setSelectedJobs] = useState<HeatmapJobData[]>([]);

  const { data: heatmapDateData, isLoading: heatmapDateLoading, error: heatmapDateError } =
    useHeatmapDateData(selectedDate ?? '');

  useEffect(() => {
    if (jobsError) console.error('Error fetching matching jobs:', jobsError);
  }, [jobsError]);

  useEffect(() => {
    setSelectedJobs(heatmapDateData?.Jobs ?? []);
  }, [heatmapDateData]);

  const handleBookmark = (id: number, targetState: boolean) => {
    bookmarkMutation.mutate({ PostingId: id, IsBookmarked: targetState });
  };

  const handleDateClick = (date: string) => {
    setSelectedDate(date);
    setModalOpened(true);
  };

  const totalApps      = dashboardData?.TotalApplications ?? 0;
  const appsThisMonth  = dashboardData?.AppsThisMonth ?? 0;
  const respRate       = dashboardData?.ResponseRate ?? 0;
  const avgRespDays    = dashboardData?.AvgResponseDays ?? 0;
  const avgRejectDays  = dashboardData?.AvgRejectionDays ?? 0;
  const pending        = dashboardData?.JobsInReview ?? 0;
  const interview      = dashboardData?.JobsInterviewStage ?? 0;

  return (
    <div className="p-4 md:p-8 min-h-screen text-neutral-200">
      {/* Heatmap date modal */}
      <Modal
        opened={modalOpened}
        onClose={() => setModalOpened(false)}
        title={`Applications on ${selectedDate}`}
        centered
        size="lg"
      >
        <div className="flex flex-col gap-3">
          {heatmapDateLoading ? (
            <p className="text-sm text-neutral-500">Loading applications…</p>
          ) : heatmapDateError ? (
            <p className="text-sm text-red-400">Error loading applications.</p>
          ) : selectedJobs.length > 0 ? (
            selectedJobs.map((job) => <JobPostingPopup key={job.JobId} job={job} />)
          ) : (
            <p className="text-sm text-neutral-500">No applications found for this date.</p>
          )}
        </div>
      </Modal>

      <div className="max-w-7xl mx-auto space-y-6">

        {/* Page header */}
        <header>
            <h1 className="text-2xl font-bold text-neutral-200 mb-2">OVERVIEW</h1>
        </header>

        {/* Stat cards */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">

          <div className="px-4 py-3.5 border border-neutral-800 rounded-xl bg-neutral-900/50 select-none">
            <p className="text-[11px] font-medium text-neutral-500 uppercase tracking-widest mb-2">Applications</p>
            <p className="text-3xl font-medium text-white">{totalApps}</p>
            <p className={`text-[12px] mt-1.5 ${appsThisMonth > 0 ? 'text-green-400' : 'text-neutral-500'}`}>
              +{appsThisMonth} this month
            </p>
          </div>

          <div className="px-4 py-3.5 border border-neutral-800 rounded-xl bg-neutral-900/50 select-none">
            <p className="text-[11px] font-medium text-neutral-500 uppercase tracking-widest mb-2">Response rate</p>
            <p className="text-3xl font-medium text-white">{respRate.toFixed()}%</p>
            <div className="flex items-center gap-3 mt-1.5">
              <Tooltip label={`Average response in ${avgRespDays}d`} position="bottom">
                <span className={`flex items-center gap-1 text-[12px] ${avgRespDays === 0 ? 'text-neutral-500' : 'text-orange-300'}`}>
                  <IconArrowWaveLeftUp size={13} />
                  {avgRespDays}d
                </span>
              </Tooltip>
              <Tooltip label={`Average rejection in ${avgRejectDays}d`} position="bottom">
                <span className={`flex items-center gap-1 text-[12px] ${avgRejectDays === 0 ? 'text-neutral-500' : 'text-red-400'}`}>
                  <IconX size={13} />
                  {avgRejectDays}d
                </span>
              </Tooltip>
            </div>
          </div>

          <div className="px-4 py-3.5 border border-neutral-800 rounded-xl bg-neutral-900/50 select-none">
            <p className="text-[11px] font-medium text-neutral-500 uppercase tracking-widest mb-2">Waiting</p>
            <p className="text-3xl font-medium text-white">{pending + interview}</p>
            <div className="flex items-center gap-2 mt-1.5 text-[12px]">
              <span className={pending > 0 ? 'text-orange-300' : 'text-neutral-500'}>{pending} pending</span>
              <span className="text-neutral-700">·</span>
              <span className={interview > 0 ? 'text-purple-400' : 'text-neutral-500'}>{interview} interview</span>
            </div>
          </div>

        </div>

        {/* Heatmap + Daily Goal */}
        <div className="grid grid-cols-1 lg:grid-cols-[1fr_260px] gap-3">

          <div className="px-4 py-3.5 border border-neutral-800 rounded-xl bg-neutral-900/50">
            <p className="text-[11px] font-medium text-neutral-500 uppercase tracking-widest mb-4 select-none">
              Application Activity
            </p>
            <CustomHeatmap
              data={heatmapData ?? {}}
              onDateClick={handleDateClick}
            />
          </div>

          <div className="px-4 py-3.5 border border-neutral-800 rounded-xl bg-neutral-900/50">
            <p className="text-[11px] font-medium text-neutral-500 uppercase tracking-widest mb-3 select-none">
              Daily Goal
            </p>
            <p className="text-2xl font-medium text-white mb-2">0 / 5</p>
            <div className="h-1 rounded-full bg-neutral-800 mb-3">
              <div className="h-full w-[0%] rounded-full bg-green-500" />
            </div>
            <p className="text-[12.5px] text-neutral-500 leading-relaxed">
              Applied to 0 jobs today — apply to 5 more to hit your daily target.
            </p>
          </div>

        </div>

        {/* Recommended jobs */}
        <RecommendedJobs
          jobs={matchingJobs ?? []}
          onBookmark={handleBookmark}
          onIgnore={(jobId) => ignoreJob({ JobId: jobId })}
          onSoftIgnore={(jobId) => softIgnoreJob({JobId: jobId})}
          isLoading={isLoading}
        />

      </div>
    </div>
  );
}