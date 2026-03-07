import { useQuery } from '@tanstack/react-query';
import { sendPhotinoRequest } from '../utils/photino';
import { GetDashboardResponse } from '../types/dashboard/get-dashboard-response';
import { GetHeatmapResponse } from '../types/dashboard/get-heatmap-response';
import { GetHeatmapDateResponse } from '../types/dashboard/get-heatmapdate-response';

export const useDashboardData = () => {
  return useQuery({
    queryKey: ['dashboard'],
    queryFn: () => sendPhotinoRequest<GetDashboardResponse>('dashboard.getInfo', { hello: "hello" }),
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
};

export const useHeatmapData = () => {
  return useQuery({
    queryKey: ['heatmap'],
    queryFn: () => sendPhotinoRequest<GetHeatmapResponse>('dashboard.getHeatmap', { hello: "hello" }),
    select: (data) => {
      // Transform heatmap API response to Record<string, number> format
      const transformedData: Record<string, number> = {};
      data.Heatmaps.forEach((item) => {
        transformedData[item.Date] = item.Applications;
      });
      return transformedData;
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

export const useHeatmapDateData = (date: string) => {
  return useQuery({
    queryKey: ['heatmap-date', date],
    queryFn: () => sendPhotinoRequest<GetHeatmapDateResponse>('dashboard.getHeatmapDate', { date: date }),
    enabled: !!date,
    staleTime: 1 * 60 * 1000, // 1 minute
  });
};
