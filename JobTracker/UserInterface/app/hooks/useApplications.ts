import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { sendPhotinoRequest } from '../utils/photino';
import { JobApplication } from '../types/applications/jobApplication';
import { CreateApplicationRequest } from '../types/applications/create-application';
import { CreateApplicationResponse } from '../types/applications/create-application';
import { UpdateApplicationRequest } from '../types/applications/update-application';
import { UpdateApplicationResponse } from '../types/applications/update-application';

export const useApplications = () => {
  return useQuery({
    queryKey: ['applications'],
    queryFn: () => sendPhotinoRequest<JobApplication[]>('applications.get', { hello: "hello" }),
    staleTime: 3 * 60 * 1000, // 3 minutes
  });
};

export const useCreateApplication = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (request: CreateApplicationRequest) => 
      sendPhotinoRequest<CreateApplicationResponse>('applications.create', request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['applications'] });
    },
  });
};

export const useUpdateApplication = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (request: UpdateApplicationRequest) => 
      sendPhotinoRequest<UpdateApplicationResponse>('applications.update', request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['applications'] });
    },
  });
};

export const useDeleteApplication = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (jobId: number) => 
      sendPhotinoRequest('applications.delete', { JobId: jobId }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['applications'] });
    },
  });
};
