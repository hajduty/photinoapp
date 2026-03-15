import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { sendPhotinoRequest } from '../utils/photino';
import { GetJobsRequest } from '../types/jobs/get-jobs-request';
import { GetJobsResponse } from '../types/jobs/get-jobs-response';
import { GetBookmarkedJobsResponse } from '../types/jobs/get-bookmarked-jobs-response';
import { BookmarkJobRequest } from '../types/jobs/bookmark-job-request';
import { BookmarkJobResponse } from '../types/jobs/bookmark-job-response';
import { JobSentenceDto } from '../types/jobs/jobsentence';
import { Classification } from '../types/classifications/classification';
import { GetFullDescriptionRequest } from '../types/jobs/get-full-description-request';
import { GetFullDescriptionResponse } from '../types/jobs/get-full-description-response';
import { ExtendedPosting } from '../types/jobs/extended-posting';

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
    onMutate: async ({ PostingId, IsBookmarked }) => {
      // Cancel in-flight queries to avoid overwriting optimistic update
      await queryClient.cancelQueries({ queryKey: ['bookmarked-jobs'] });
      await queryClient.cancelQueries({ queryKey: ['jobs'] });

      // Snapshot previous values for rollback
      const previousBookmarked = queryClient.getQueryData(['bookmarked-jobs']);

      // Optimistically update the cache
      queryClient.setQueryData(['bookmarked-jobs'], (old: GetBookmarkedJobsResponse) => {
        if (!old) return old;
        return {
          ...old,
          TaggedPostings: old.TaggedPostings.map((posting) =>
            posting.Posting.Id === PostingId
              ? { ...posting, Posting: { ...posting.Posting, Bookmarked: IsBookmarked } }
              : posting
          ),
        };
      });

      return { previousBookmarked };
    },
    onError: (_err, _vars, context) => {
      // Roll back on error
      if (context?.previousBookmarked) {
        queryClient.setQueryData(['bookmarked-jobs'], context.previousBookmarked);
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['jobs'] });
      queryClient.invalidateQueries({ queryKey: ['bookmarked-jobs'] });
      queryClient.invalidateQueries({ queryKey: ['matching-jobs'] });
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

export const useFullDescription = (jobId: number) => {
  return useQuery({
    queryKey: ['full-description', jobId],
    queryFn: () => sendPhotinoRequest<GetFullDescriptionResponse>('jobs.getFullDescription', { JobId: jobId }),
    enabled: !!jobId,
    staleTime: 10 * 60 * 1000, // 10 minutes
  });
};

export const useMatchingJobs = () => {
  return useQuery({
    queryKey: ['matching-jobs'],
    queryFn: () => sendPhotinoRequest<ExtendedPosting[]>('jobs.getMatchingJobs', {}),
    staleTime: 1 * 60 * 1000, // 1 minute
  });
};
