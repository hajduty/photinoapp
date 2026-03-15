import { useMutation, useQueryClient } from '@tanstack/react-query';
import { sendPhotinoRequest } from '../utils/photino';
import { IgnoreJobRequest } from '../types/jobs/ignore-job-request';
import { IgnoreJobResponse } from '../types/jobs/ignore-job-response';

export const useIgnoreJob = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (request: IgnoreJobRequest) => 
      sendPhotinoRequest<IgnoreJobResponse>('jobs.ignore', request),
    onSuccess: () => {
      // Invalidate relevant queries to refresh the data
      queryClient.invalidateQueries({ queryKey: ['jobs'] });
      queryClient.invalidateQueries({ queryKey: ['matching-jobs'] });
    },
  });
};