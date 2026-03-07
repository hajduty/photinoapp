import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { sendPhotinoRequest } from '../utils/photino';
import { JobTracker } from '../types/jobs/job-tracker';
import { CreateJobTrackerRequest } from '../types/jobs/create-job-tracker-request';
import { CreateJobTrackerResponse } from '../types/jobs/create-job-tracker-response';
import { UpdateJobTrackerRequest } from '../types/jobs/update-job-tracker-request';
import { UpdateJobTrackerResponse } from '../types/jobs/update-job-tracker-response';
import { DeleteJobTrackerRequest } from '../types/tag/delete-job-tracker-request';
import { DeleteJobTrackerResponse } from '../types/tag/delete-job-tracker-response';
import { Tag } from '../types/tag/tag';

export const useTrackers = () => {
  return useQuery({
    queryKey: ['trackers'],
    queryFn: () => sendPhotinoRequest<JobTracker[]>('jobTracker.getTrackers', { hello: "hello" }),
    staleTime: 3 * 60 * 1000, // 3 minutes
  });
};

export const useAvailableTags = () => {
  return useQuery({
    queryKey: ['available-tags'],
    queryFn: () => sendPhotinoRequest<Tag[]>('tags.getTags', { hello: "hello" }),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

export const useCreateTracker = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (request: CreateJobTrackerRequest) => 
      sendPhotinoRequest<CreateJobTrackerResponse>('jobTracker.createTracker', request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['trackers'] });
    },
  });
};

export const useUpdateTracker = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (request: UpdateJobTrackerRequest) => 
      sendPhotinoRequest<UpdateJobTrackerResponse>('jobTracker.updateTracker', request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['trackers'] });
    },
  });
};

export const useDeleteTracker = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (request: DeleteJobTrackerRequest) => 
      sendPhotinoRequest<DeleteJobTrackerResponse>('jobTracker.deleteTracker', request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['trackers'] });
    },
  });
};

export const useProcessTracker = () => {
  return useMutation({
    mutationFn: () => 
      sendPhotinoRequest('jobTracker.process', { Data: "" }),
  });
};