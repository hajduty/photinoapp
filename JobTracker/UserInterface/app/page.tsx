'use client';

import { useState, useEffect } from 'react';
import { Grid, SimpleGrid, Text, Modal, Tooltip } from "@mantine/core";
import JobPostingPopup from './features/search/JobPostingPopup';
import { sendPhotinoRequest } from './utils/photino';
import { CustomHeatmap } from './features/dashboard/Heatmap';
import { GetDashboardResponse } from './types/dashboard/get-dashboard-response';
import { IconArrowWaveLeftUp, IconX, IconBriefcase, IconMail, IconClock } from '@tabler/icons-react';
import { GetHeatmapResponse } from './types/dashboard/get-heatmap-response';
import { GetHeatmapDateResponse, HeatmapJobData } from './types/dashboard/get-heatmapdate-response';

export default function Dashboard() {
  const [selectedDate, setSelectedDate] = useState<string | null>(null);
  const [modalOpened, setModalOpened] = useState(false);
  const [dashboardData, setDashboardData] = useState<GetDashboardResponse | null>(null);
  const [heatmapData, setHeatmapData] = useState<Record<string, number>>({});
  const [selectedJobs, setSelectedJobs] = useState<HeatmapJobData[]>([]);

  useEffect(() => {
    const fetchDashboardData = async () => {
      try {
        const [dashboard, heatmap] = await Promise.all([
          sendPhotinoRequest<GetDashboardResponse>('dashboard.getInfo', {}),
          sendPhotinoRequest<GetHeatmapResponse>('dashboard.getHeatmap', {})
        ]);

        setDashboardData(dashboard);
        
        // Transform heatmap API response to Record<string, number> format
        const transformedData: Record<string, number> = {};
        heatmap.Heatmaps.forEach((item) => {
          transformedData[item.Date] = item.Applications;
        });
        setHeatmapData(transformedData);
      } catch (err) {
        console.error('Failed to load dashboard data:', err);
      }
    };

    fetchDashboardData();
  }, []);

  const showInfoForDate = async (date: string) => {
    var response = await sendPhotinoRequest<GetHeatmapDateResponse>("dashboard.getHeatmapDate", {date});
    setSelectedJobs(response.Jobs);
  }

  const totalApps = dashboardData?.TotalApplications ?? 0;
  const appsThisMonth = dashboardData?.AppsThisMonth ?? 0;
  const respRate = dashboardData?.ResponseRate ?? 0;
  const avgResponseDays = dashboardData?.AvgResponseDays ?? 0;
  const avgRejectionDays = dashboardData?.AvgRejectionDays ?? 0;
  const pending = dashboardData?.JobsInReview ?? 0;
  const interview = dashboardData?.JobsInterviewStage ?? 0;

  return (
    <div className="p-4 md:p-8 min-h-screen text-neutral-200">
      <Modal opened={modalOpened} onClose={() => setModalOpened(false)} title={`Applications on ${selectedDate}`} centered size="lg">
        <div className="flex flex-col gap-3">
          {selectedJobs.length > 0 ? selectedJobs.map((job) => <JobPostingPopup key={job.JobId} job={job} />) : <Text c="dimmed">No applications found for this date.</Text>}
        </div>
      </Modal>

      <div className="max-w-7xl mx-auto space-y-8">
        <header><h1 className="text-2xl font-bold text-white">OVERVIEW</h1></header>

        <SimpleGrid cols={{ base: 1, sm: 3 }} spacing="lg">
          {/* APPLICATIONS */}
          <div className="p-6 border border-neutral-800 rounded-xl bg-neutral-900/70 select-none relative overflow-hidden group">
            <IconBriefcase size={180} className="absolute end-0 top-1/2 -translate-y-1/2 text-neutral-700 opacity-10 group-hover:opacity-15 transition-opacity blur-sm" />
            <div className="relative z-10">
              <div className="text-neutral-500 text-xs font-bold mb-1"> APPLICATIONS</div>
              <div className="text-4xl font-black text-white">{totalApps}</div>
              <div className={`text-sm mt-2 ${appsThisMonth > 0 ? 'text-green-400' : ''}`}>+{appsThisMonth} this month</div>
            </div>
          </div>

          {/* RESP RATE */}
          <div className="p-6 border border-neutral-800 rounded-xl bg-neutral-900/70 select-none relative overflow-hidden group">
            <IconMail size={180} className="absolute end-0 top-1/2 -translate-y-1/2 text-neutral-700 opacity-10 group-hover:opacity-15 transition-opacity blur-sm" />
            <div className="relative z-10">
              <div className="text-neutral-500 text-xs font-bold mb-1">RESP RATE</div>
              <div className="text-4xl font-black text-white">{respRate.toFixed()}%</div>
              <div className="text-sm mt-2 flex gap-3">
                <Tooltip label={`You get a response on average ${avgResponseDays} days after submitting.`} position="left">
                  <span className={`${avgResponseDays === 0 ? 'text-green-400' : 'text-orange-300'} flex items-center gap-1 select-none`}>
                    <IconArrowWaveLeftUp size={16} />
                    {avgResponseDays}d
                  </span>
                </Tooltip>
                <Tooltip label={`You get a response on average ${avgRejectionDays} days after submitting.`} position="right">
                  <span className={`${avgRejectionDays === 0 ? 'text-red-400' : 'text-orange-300'} flex items-center select-none`}>
                    <IconX size={16} />
                    {avgRejectionDays}d
                  </span>
                </Tooltip>
              </div>
            </div>
          </div>

          {/* WAITING */}
          <div className="p-6 border border-neutral-800 rounded-xl bg-neutral-900/70 select-none relative overflow-hidden group">
            <IconClock size={180} className="absolute end-0 top-1/2 -translate-y-1/2 text-neutral-700 opacity-10 group-hover:opacity-15 transition-opacity blur-sm" />
            <div className="relative z-10">
              <div className="text-neutral-500 text-xs font-bold mb-1">WAITING</div>
              <div className="text-4xl font-black text-white">{pending + interview}</div>
              <div className="text-sm mt-2 flex gap-3">
                <span className={pending > 0 ? 'text-orange-300' : ''}>{pending} pending</span>
                <span className={interview > 0 ? 'text-purple-400' : ''}>{interview} interview</span>
              </div>
            </div>
          </div>
        </SimpleGrid>

        <Grid gutter="lg">
          <Grid.Col span={{ base: 12, lg: 8 }}>
            <div className="p-6 border border-neutral-800 rounded-xl bg-neutral-900/50">
              <div className="mb-6"><Text size="xs" fw={700} c="dimmed" className="uppercase tracking-widest select-none">Application Activity</Text></div>
              <CustomHeatmap data={heatmapData} onDateClick={(date) => { setSelectedDate(date); setModalOpened(true); showInfoForDate(date); }} />
            </div>
          </Grid.Col>
          <Grid.Col span={{ base: 12, lg: 4 }}></Grid.Col>
        </Grid>
      </div>
    </div>
  );
}
