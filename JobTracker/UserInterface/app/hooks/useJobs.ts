import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { sendPhotinoRequest } from '../utils/photino';
import { GetJobsRequest } from '../types/jobs/get-jobs-request';
import { GetJobsResponse } from '../types/jobs/get-jobs-response';
import { GetBookmarkedJobsResponse } from '../types/jobs/get-bookmarked-jobs-response';
import { BookmarkJobRequest } from '../types/jobs/bookmark-job-request';
import { BookmarkJobResponse } from '../types/jobs/bookmark-job-response';
import { JobSentenceDto } from '../types/jobs/jobsentence';
import { Classification } from '../types/classifications/classification';

export const useJobs = (request: GetJobsRequest) => {
  return useQuery({
    queryKey: ['jobs', request],
    queryFn: () => sendPhotinoRequest<GetJobsResponse>('jobs.getJobs', request),
    staleTime: 3 * 60 * 1000, // 3 minutes
  });
};

export const useBookmarkedJobs = () => {
  return useQuery({
    queryKey: ['bookmarked-jobs'],
    queryFn: () => sendPhotinoRequest<GetBookmarkedJobsResponse>('jobs.getBookmarked', { hello: "hello" }),
    staleTime: 3 * 60 * 1000, // 3 minutes
  });
};

export const useBookmarkJob = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (request: BookmarkJobRequest) => 
      sendPhotinoRequest<BookmarkJobResponse>('jobs.bookmark', request),
    onSuccess: () => {
      // Invalidate relevant queries to refetch data
      queryClient.invalidateQueries({ queryKey: ['jobs'] });
      queryClient.invalidateQueries({ queryKey: ['bookmarked-jobs'] });
    },
  });
};

export const useJobDescription = (jobId: number) => {
  return useQuery({
    queryKey: ['job-description', jobId],
    queryFn: () => sendPhotinoRequest<JobSentenceDto[]>('embeddings.getDescription', { JobId: jobId }),
    enabled: !!jobId,
    staleTime: 10 * 60 * 1000, // 10 minutes
  });
};

export const useClassifications = () => {
  return useQuery({
    queryKey: ['classifications'],
    queryFn: () => sendPhotinoRequest<Classification[]>('classifications.get', {hello:"hello"}),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};